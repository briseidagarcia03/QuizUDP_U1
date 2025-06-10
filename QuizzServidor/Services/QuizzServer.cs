using QuizzServidor.Models;
using QuizzServidor.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace QuizzServidor.Services
{
    public class QuizzServer
    {
        public UdpClient server;
        public event EventHandler<RespuestaDTO>? RespuestaRecibida;
        public event EventHandler<Usuario>? UsuarioRegistrado;
        public event EventHandler<string>? UsuarioRechazado;
        public event EventHandler<string>? UsuarioDesconectado;
        public event EventHandler<string>? ErrorOcurrido;

        private Timer? siguevivotimer;


        public ObservableCollection<Usuario> UsuariosConectados { get; set; } = new ObservableCollection<Usuario>();


        public QuizzServer()
        {
            server = new UdpClient(new IPEndPoint(IPAddress.Any, 11000));
            var hilo = new Thread(new ThreadStart(EscucharRespuestas))
            {
                IsBackground = true
            };
            hilo.Start();

            siguevivotimer = new Timer(MantenerConexion, null, 5000, 12000);

        }

        private void MantenerConexion(object? state)
        {
            if (UsuariosConectados.Count > 0)
            {
                var now = DateTime.Now;
                var usuariosADesconectarNombres = new List<string>();
          

            
            foreach (var usuario in UsuariosConectados)
            {
                if ((now - usuario.UltimoHeartbeat).TotalSeconds > 15)
                {
                    usuariosADesconectarNombres.Add(usuario.Nombre);
                }
            }


            if (usuariosADesconectarNombres.Any())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var nombreUsuario in usuariosADesconectarNombres)
                    {

                        var usuarioToRemove = UsuariosConectados.FirstOrDefault(u => u.Nombre == nombreUsuario);
                        if (usuarioToRemove != null)
                        {
                            UsuariosConectados.Remove(usuarioToRemove); 
                            UsuarioDesconectado?.Invoke(this, usuarioToRemove.Nombre);
                        }
                    }
                });
            }
           }
        }

        private void EscucharRespuestas()
        {
            IPEndPoint? remoto = null;
            try
            {
                while (true)
                {
                    try
                    {
                        byte[] buffer = server.Receive(ref remoto);
                        string json = Encoding.UTF8.GetString(buffer);

                        // Deserializa solo el tipo
                        var tipoMensaje = JsonSerializer.Deserialize<Base>(json);

                        switch (tipoMensaje.Tipo)
                        {
                            case "UsuarioConectado":
                                var usuario = JsonSerializer.Deserialize<Usuario>(json);
                                if (usuario != null && remoto != null)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        if (UsuariosConectados.Any(u => u.Nombre == usuario.Nombre))
                                        {
                                            EnviarMensaje(new Base { Tipo = "NombreDuplicado" }, remoto);
                                            UsuarioRechazado?.Invoke(this, usuario.Nombre);
                                        }
                                        else
                                        {
                                            usuario.RemoteEndPoint = remoto;
                                            usuario.UltimoHeartbeat = DateTime.Now; // Actualiza el ultimo heartbeat
                                            UsuariosConectados.Add(usuario);
                                            EnviarMensaje(new Base { Tipo = "UsuarioAceptado" }, remoto);
                                            UsuarioRegistrado?.Invoke(this, usuario);
                                            
                                        }
                                    });
                                }
                                break;

                            case "Respuesta":
                                var respuesta = JsonSerializer.Deserialize<RespuestaDTO>(json);
                                if (respuesta != null)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        RespuestaRecibida?.Invoke(this, respuesta);
                                    });
                                }
                                break;

                            case "Heartbeat":
                                
                                var hb = JsonSerializer.Deserialize<HeartBeatDTO>(json);
                                
                                if(hb != null)
                                {
                                    var user = UsuariosConectados.FirstOrDefault(u => u.Nombre == hb.NombreUsuario);
                                    if(user != null)
                                    {
                                        user.UltimoHeartbeat = DateTime.Now;
                                    }
                                }
                                break;

                            case "Ping":
                                EnviarMensaje(new Base { Tipo = "Pong" }, remoto); // Si le llega un ping entonces responde con un pong
                                break;


                        }
                    }
                    catch (SocketException ex)
                    {
                        if(UsuariosConectados.Count == 0)
                        {
                            ErrorOcurrido?.Invoke(this, $"SinJugadores");
                        }
                    }
                    
                }
            }
            catch (Exception)
            {
                ErrorOcurrido?.Invoke(this, $"ErrorInesperado");
            }
        }

        public void EnviarMensaje(object mensaje, IPEndPoint destino)
        {
            try
            {
                string json = JsonSerializer.Serialize(mensaje);
                byte[] data = Encoding.UTF8.GetBytes(json);
                server.Send(data, data.Length, destino);
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al enviar mensaje a {destino}: {ex.Message}");
            }
        }



        public void EnviarPregunta(PreguntaDTO pregunta)
        {
            try
            {
                foreach (var usuario in UsuariosConectados)
                {
                    if (usuario.RemoteEndPoint != null)
                    {
                        EnviarMensaje(pregunta, usuario.RemoteEndPoint);
                    }
                }
            }
            catch (Exception)
            {

                ErrorOcurrido?.Invoke(this, $"ErrorInesperado");
            }

        }

        public void EnviarParaTodos(string tipo)
        {
            try
            {
                foreach (var usuario in UsuariosConectados)
                {
                    if (usuario.RemoteEndPoint != null)
                    {
                        EnviarMensaje(new Base { Tipo = tipo }, usuario.RemoteEndPoint);
                    }
                }
            }
            catch (Exception)
            {
                ErrorOcurrido?.Invoke(this, $"ErrorInesperado");
            }

        }

        public void EnviarResultadosParciales(ResultadosParcialesDTO parciales)
        {
            try
            {
                foreach (var usuario in UsuariosConectados)
                {
                    if (usuario.RemoteEndPoint != null)
                    {
                        EnviarMensaje(parciales, usuario.RemoteEndPoint);
                    }
                }
            }
            catch (Exception)
            {
                ErrorOcurrido?.Invoke(this, $"ErrorInesperado");
            }

        }

        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
