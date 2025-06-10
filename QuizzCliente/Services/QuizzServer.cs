using QuizzCliente.Models;
using QuizzCliente.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace QuizzCliente.Services
{
    public class QuizzServer
    {
        private UdpClient udpClient;
        private string servidorIP;
        private int puerto = 11000;
        public string nombreUsuario;
        private DispatcherTimer siguevivotimer;
        private System.Timers.Timer? pingtimeoutTimer;
        private bool seguirEscuchando = true;

        public event EventHandler<PreguntaDTO>? PreguntaRecibida;
        public event EventHandler<string>? NotificacionRecibida;
        public event EventHandler<string>? ValidacionNombreRecibida;
        public event EventHandler<ResultadosParcialesDTO>? ResultadosParcialesRecibidos;
        public QuizzServer(string servidorIP)
        {
            this.servidorIP = servidorIP;
            siguevivotimer = new DispatcherTimer();
            siguevivotimer.Interval = TimeSpan.FromSeconds(8);
            siguevivotimer.Tick += Siguevivotimer_Tick;
            IniciarSocketYReceptor();
        }

        private void IniciarSocketYReceptor()
        {
            udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            seguirEscuchando = true;
            Thread hilo = new Thread(EscucharServidor)
            {
                IsBackground = true
            };
            hilo.Start();
        }


        private void Siguevivotimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(nombreUsuario)) return; // No enviar heartbeat sin nombre
                var hb = new HeartBeatDTO { Tipo = "Heartbeat", NombreUsuario = nombreUsuario };
                string json = JsonSerializer.Serialize(hb);
                byte[] data = Encoding.UTF8.GetBytes(json);
                udpClient.Send(data, data.Length, servidorIP, puerto);

            }
            catch (Exception)
            {
                throw new ApplicationException("Error al verificar si el usuario sigue en linea.");
            }
        }

        public void EnviarUsuarioConectado()
        {
            var usuario = new Usuario
            {
                Tipo = "UsuarioConectado",
                Nombre = nombreUsuario,
            };

            var json = JsonSerializer.Serialize(usuario);

            byte[] data = Encoding.UTF8.GetBytes(json);
            udpClient.Send(data, data.Length, servidorIP, puerto);
        }

        public void EstablecerNombre(string nombre)
        {
            nombreUsuario = nombre;
        }

        public void EnviarRespuesta(int respuestaSeleccionada)
        {
            var respuesta = new RespuestaDTO
            {
                Usuario = nombreUsuario,
                RespuestaUsuario = respuestaSeleccionada,
                Tipo = "Respuesta"
            };

            var json = JsonSerializer.Serialize(respuesta);
            byte[] data = Encoding.UTF8.GetBytes(json);
            udpClient.Send(data, data.Length, servidorIP, 11000);
        }

        public void EnviarPing(string ip)
        {
            try
            {
                ActualizarIP(ip); // Cambia IP y reinicia escucha

                var ping = new Base { Tipo = "Ping" };
                string json = JsonSerializer.Serialize(ping);
                byte[] data = Encoding.UTF8.GetBytes(json);

                udpClient.Send(data, data.Length, servidorIP, puerto);

                pingtimeoutTimer?.Stop();
                pingtimeoutTimer = new System.Timers.Timer(5000);
                pingtimeoutTimer.Elapsed += (sender, e) =>
                {
                    NotificacionRecibida?.Invoke(this, "PingTimeout");
                    pingtimeoutTimer.Stop();
                };
                pingtimeoutTimer.AutoReset = false;
                pingtimeoutTimer.Start();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error al enviar el ping.");
            }
        }


        private void EscucharServidor()
        {
            try
            {
                while (true)
                {
                    IPEndPoint remoto = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref remoto);
                    string json = Encoding.UTF8.GetString(data);

                    var baseMensaje = JsonSerializer.Deserialize<Base>(json);
                    if (baseMensaje == null) continue;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        switch (baseMensaje.Tipo)
                        {
                            case "Pregunta":
                                var pregunta = JsonSerializer.Deserialize<PreguntaDTO>(json);
                                if (pregunta != null)
                                    PreguntaRecibida?.Invoke(this, pregunta);
                                break;

                            case "NombreDuplicado":
                                    ValidacionNombreRecibida?.Invoke(this, "Duplicado");
                                break;

                            case "UsuarioAceptado":
                                    ValidacionNombreRecibida?.Invoke(this, "Aceptado");
                                siguevivotimer.Start();
                                break;

                            case "Pong": // El cliente recibe el Pong aquí y notifica.
                                NotificacionRecibida?.Invoke(this, "Pong");
                                pingtimeoutTimer.Stop();
                                break;

                            case "ResultadosParciales":
                                var resultadosParciales = JsonSerializer.Deserialize<ResultadosParcialesDTO>(json);
                                if (resultadosParciales != null)
                                {
                                    ResultadosParcialesRecibidos?.Invoke(this, resultadosParciales);
                                }
                                break;

                            case "ResultadosFinales":
                                    NotificacionRecibida?.Invoke(this, "ResultadosFinales");

                                break;
                            case "IniciarJuego":
                                NotificacionRecibida?.Invoke(this, "IniciarJuego");
                                break;

                            case "ReiniciarJuego":
                                NotificacionRecibida?.Invoke(this, "ReiniciarJuego");
                                siguevivotimer.Stop(); // Detener el timer al recibir resultados finales

                                break;
                            case "TiempoPreguntaFinalizado":
                                NotificacionRecibida?.Invoke(this, "TiempoPreguntaFinalizado");
                                break;

                        }
                    });
                   
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void ActualizarIP(string nuevaIP)
        {
            try
            {
                seguirEscuchando = false;
                Thread.Sleep(50); // Dar tiempo a que el hilo termine

                udpClient?.Close(); // Forzar cierre del socket para romper el Receive bloqueado

                servidorIP = nuevaIP;

                IniciarSocketYReceptor(); // Reiniciar todo con la nueva IP
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error un error al actualizar la IP.");
            }
        }
    }
}
