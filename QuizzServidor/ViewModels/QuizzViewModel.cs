using GalaSoft.MvvmLight.Command;
using QuizzServidor.Models;
using QuizzServidor.Models.DTOs;
using QuizzServidor.Services;
using QuizzServidor.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace QuizzServidor.ViewModels
{
    public class QuizzViewModel : INotifyPropertyChanged
    {

        private QuizzServer quizzServer;

        private System.Timers.Timer? lecturaTimer;
        private DispatcherTimer? contadorVisual;

        public List<Usuario> TopUsuarios { get; set; } = new List<Usuario>();
        public double PorcentajeAciertos { get; set; } = 0.0;
        public string Mensaje { get; set; } = string.Empty;

        public double TiempoEstablecido { get; set; } 
        public int TiempoRestante { get; set; } = 0;
        public int CantidadPreguntas { get; set; } = 0;
        public bool ActivarBotones { get; set; } = false;

        private int preguntaIndice = 0;
        public int CantidadJugadoresConectados { get; set; } = 0;
        public string IP { get; set; } = "0.0.0.0";        
        public UserControl VistaActual { get; set; }
        public PreguntaDTO PreguntaActual { get; set; }

        public ObservableCollection<ResultadoParcialItem> ResultadosParciales { get; set; } = new();
        public ObservableCollection<Usuario> UsuariosConectados => quizzServer.UsuariosConectados;
        public List<PreguntaDTO> Preguntas { get; set; } = new();
        public ObservableCollection<string> OpcionesPregunta { get; set; } = new();
        public string Respuesta1 { get; set; } = null!;
        public string Respuesta2 { get; set; } = null!;
        public string Respuesta3 { get; set; } = null!;
        public string Respuesta4 { get; set; } = null!;

        public ICommand SiguientePreguntaCommand { get; set; }
        public ICommand IniciarCommand { get; set; }
        public ICommand FinalizarCommand { get; set; }
        public ICommand CrearPreguntaCommand { get; set; }

        public QuizzViewModel()
        {
            VistaActual = new AgregarPreguntaView();
            PreguntaActual = new();
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            quizzServer = new QuizzServer();
            quizzServer.RespuestaRecibida += QuizzServer_RespuestaRecibida;
            quizzServer.UsuarioRegistrado += QuizzServer_UsuarioRegistrado;
            quizzServer.UsuarioDesconectado += QuizzServer_UsuarioDesconectado;
            quizzServer.ErrorOcurrido += QuizzServer_ErrorOcurrido;


            IP = ips.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).
                Select(x => x.ToString()).FirstOrDefault() ?? "0.0.0.0";

            lecturaTimer = new System.Timers.Timer();
            lecturaTimer.AutoReset = false;
            lecturaTimer.Elapsed += LecturaTimer_Elapsed;

            contadorVisual = new DispatcherTimer();
            contadorVisual.Interval = TimeSpan.FromSeconds(1);
            contadorVisual.Tick += ContadorVisual_Tick;

            IniciarCommand = new RelayCommand(Iniciar);
            SiguientePreguntaCommand = new RelayCommand(SiguientePregunta);
            FinalizarCommand = new RelayCommand(Finalizar);
            CrearPreguntaCommand = new RelayCommand(CrearPregunta);

        }

        private void QuizzServer_ErrorOcurrido(object? sender, string e)
        {
            if(e == "SinJugadores")
            {
                MessageBox.Show("No hay jugadores conectados, se cerrará la sesión.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e == "ErrorInesperado")
            {
                MessageBox.Show("Ocurrió un error inesperado. Por favor, reinicia la aplicación.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Finalizar();
            ActualizarDatos();
        }

        private async void ContadorVisual_Tick(object? sender, EventArgs e)
        {
            TiempoRestante--;
            ActualizarDatos(nameof(TiempoRestante));

            if (TiempoRestante <= 0)
            {
                contadorVisual.Stop();
                ActivarBotones = false;
                quizzServer.EnviarParaTodos("TiempoPreguntaFinalizado");
                await Task.Delay(500);
                ProcesarRespuestas();
            }
        }

        public void ProcesarRespuestas()
        {
            Task.Run(() =>
            {
                var resultados = new ResultadosParcialesDTO
                {
                    Tipo = "ResultadosParciales",
                    TextoPregunta = PreguntaActual.Texto,
                    IndiceRespuestaCorrecta = PreguntaActual.RespuestaCorrecta,
                    OpcionesPregunta = PreguntaActual.Opciones,
                    VotosPorOpcion = new Dictionary<int, int>(),
                    AcertaronUsuarios = new List<string>(),
                    FallaronUsuarios = new List<string>(),
                    NoRespondieronUsuarios = new List<string>()
                };

                for (int i = 0; i < PreguntaActual.Opciones.Count; i++)
                    resultados.VotosPorOpcion[i] = 0;

                foreach (var user in UsuariosConectados)
                {
                    if (user.RespuestaActual.HasValue)
                    {
                        int respuesta = user.RespuestaActual.Value;
                        if (respuesta >= 0 && respuesta < PreguntaActual.Opciones.Count)
                        {
                            resultados.VotosPorOpcion[respuesta]++;
                            if (respuesta == PreguntaActual.RespuestaCorrecta)
                            {
                                user.Aciertos++;
                                resultados.AcertaronUsuarios.Add(user.Nombre);
                            }
                            else 
                            {
                                resultados.FallaronUsuarios.Add(user.Nombre);
                            }
                        }
                        else
                        {
                            resultados.NoRespondieronUsuarios.Add(user.Nombre);
                        }
                        user.RespuestaActual = null;
                    }
                }

                quizzServer.EnviarResultadosParciales(resultados);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ResultadosParciales.Clear();
                    ResultadosParciales.Add(new ResultadoParcialItem
                    {
                        Titulo = "Pregunta",
                        Contenido = resultados.TextoPregunta + "\nRespuesta Correcta: " + resultados.OpcionesPregunta[resultados.IndiceRespuestaCorrecta]
                    });

                    //Votos por Opción
                    var votosTexto = new System.Text.StringBuilder();
                    foreach (var entry in resultados.VotosPorOpcion.OrderBy(kv => kv.Key))
                    {
                        var textoOpcion = resultados.OpcionesPregunta[entry.Key];
                        votosTexto.AppendLine($"{textoOpcion} - {entry.Value} votos");
                    }
                    ResultadosParciales.Add(new ResultadoParcialItem
                    {
                        Titulo = "Votos por Opción",
                        Contenido = votosTexto.ToString().Trim()
                    });

                    //Usuarios que acertaron, fallaron o no respondieron
                    ResultadosParciales.Add(new ResultadoParcialItem { Titulo = "Jugadores que acertaron", Contenido = string.Join(", ", resultados.AcertaronUsuarios) });
                    ResultadosParciales.Add(new ResultadoParcialItem { Titulo = "Jugadores que fallaron", Contenido = string.Join(", ", resultados.FallaronUsuarios) });
                    ResultadosParciales.Add(new ResultadoParcialItem { Titulo = "Jugadores que no respondieron", Contenido = string.Join(", ", resultados.NoRespondieronUsuarios) });

                    VistaActual = new ResultadosParcialesView
                    {
                        DataContext = ResultadosParciales
                    };
                    ActualizarDatos();
                    preguntaIndice++;
                });

                Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SiguientePregunta();
                    });
                });
            });
        }

        private void LecturaTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TiempoRestante = (int)PreguntaActual.TiempoLimite;
                contadorVisual.Start();
                ActivarBotones = true;
                ActualizarDatos();
            });
        }

        private void QuizzServer_UsuarioDesconectado(object? sender, string e)
        {
            CantidadJugadoresConectados--;
            ActualizarDatos();
        }

        private void QuizzServer_UsuarioRegistrado(object? sender, Usuario e)
        {
            CantidadJugadoresConectados++;
            ActualizarDatos();
        }

        private void QuizzServer_RespuestaRecibida(object? sender, RespuestaDTO e)
        {
            var user = UsuariosConectados.FirstOrDefault(u => u.Nombre == e.Usuario);
            if(user != null)
            {

                user.RespuestaActual = e.RespuestaUsuario;
                user.UltimoHeartbeat = DateTime.Now;
                ActualizarDatos();
            }
        }

        private void Iniciar()
        {
            if (UsuariosConectados.Count > 0)
            {
                preguntaIndice = 0;
                ResultadosParciales.Clear();
                quizzServer.EnviarParaTodos("IniciarJuego");
                SiguientePregunta();
                VistaActual = new PreguntaActivaView();
            }
            else 
            {   
                Mensaje = "No hay usuarios conectados para iniciar el juego.";
            }
            ActualizarDatos();
        }

        private void ResultadosFinales()
        {
            TopUsuarios = UsuariosConectados.OrderByDescending(x => x.Aciertos).ToList();
            PorcentajeAciertos = TopUsuarios.Count > 0 ? TopUsuarios.Average(u => (double)u.Aciertos / CantidadPreguntas * 100): 0;
            VistaActual = new ResultadosFinalesView();
            quizzServer.EnviarParaTodos("ResultadosFinales");
            ActualizarDatos();
        }

        private void CrearPregunta()
        {

            if (string.IsNullOrWhiteSpace(PreguntaActual.Texto) ||
                string.IsNullOrWhiteSpace(Respuesta1) ||
                string.IsNullOrWhiteSpace(Respuesta2) ||
                string.IsNullOrWhiteSpace(Respuesta3) ||
                string.IsNullOrWhiteSpace(Respuesta4) ||
                TiempoEstablecido <= 0)
            {
                Mensaje = "Por favor, completa todos los campos de la pregunta y el tiempo.";
                ActualizarDatos();
                return;
            }

            if(TiempoEstablecido > 120 || TiempoEstablecido < 5)
            {
                Mensaje = "El tiempo de respuesta debe ser mayor a 5 segundos y menor a 2 minutos (120 segundos).";
                ActualizarDatos();
                return;

            }


                Preguntas.Add(new PreguntaDTO
                {
                    Tipo = "Pregunta",
                    Texto = PreguntaActual.Texto,
                    Opciones = new List<string> { Respuesta1, Respuesta2, Respuesta3, Respuesta4 },
                    RespuestaCorrecta = 0,
                    TiempoLimite = TiempoEstablecido
                });

                CantidadPreguntas = Preguntas.Count;
                PreguntaActual.Texto = string.Empty;
                TiempoEstablecido = 0;
                Respuesta1 = string.Empty;
                Respuesta2 = string.Empty;
                Respuesta3 = string.Empty;
                Respuesta4 = string.Empty;

            if (CantidadPreguntas >= 5)
                {
                    ActivarBotones = true;
                }
            Mensaje = string.Empty;
            ActualizarDatos();

        }

        private void Finalizar()
        {
            contadorVisual?.Stop();
            lecturaTimer?.Stop();
            PreguntaActual = new();
            CantidadPreguntas = 0;
            CantidadJugadoresConectados = 0;
            ActivarBotones = false;
            Preguntas.Clear();
            Mensaje = string.Empty;
            quizzServer.EnviarParaTodos("ReiniciarJuego");
            UsuariosConectados.Clear();
            quizzServer.UsuariosConectados.Clear();
            VistaActual = new AgregarPreguntaView();
            ActualizarDatos();
        }

        private void SiguientePregunta()
        {
            if(UsuariosConectados.Count  > 0)
            {

            
            if (preguntaIndice >= Preguntas.Count)
            {
                ResultadosFinales();
                return;
            }

            if (VistaActual is ResultadosParcialesView)
            {
                VistaActual = new PreguntaActivaView();
            }

            PreguntaActual = Preguntas[preguntaIndice];
            PreguntaActual.HoraInicioUTC = DateTime.UtcNow;
            MezclarOpciones(PreguntaActual);
            

            OpcionesPregunta.Clear();
            foreach (var opcion in PreguntaActual.Opciones)
            {
                OpcionesPregunta.Add(opcion);
            }

            TiempoRestante = (int)PreguntaActual.TiempoLimite;
            lecturaTimer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            lecturaTimer.Start();
            ActualizarDatos();

            }
            else
            {
                MessageBox.Show("No hay ningun jugador conectado, asi que la partida se da por terminada.", "Partida finalizada", MessageBoxButton.OK, MessageBoxImage.Warning);
                Finalizar();

            }
        }

        private void MezclarOpciones(PreguntaDTO pregunta)
        {
            var rnd = new Random();
            var opcionesOriginales = new List<string>(pregunta.Opciones);
            var respuestaCorrecta = opcionesOriginales[pregunta.RespuestaCorrecta];

            int n = pregunta.Opciones.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n+1);
                string valor = pregunta.Opciones[k];
                pregunta.Opciones[k] = pregunta.Opciones[n];
                pregunta.Opciones[n] = valor;
            }

            pregunta.RespuestaCorrecta = pregunta.Opciones.IndexOf(respuestaCorrecta);
            quizzServer.EnviarPregunta(pregunta);
        }

        private void ActualizarDatos(string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
