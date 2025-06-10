using GalaSoft.MvvmLight.Command;
using QuizzCliente.Models.DTOs;
using QuizzCliente.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace QuizzCliente.ViewModels
{
    public class QuizzViewModel:INotifyPropertyChanged
    {
        private QuizzServer? quizzService;
        private System.Timers.Timer? lecturaTimer;
        public PreguntaDTO PreguntaActual { get; set; }
        public int Aciertos { get; set; } = 0;
        public int OpcionSeleccionada { get; set; } = -1;
        public string Mensaje { get; set; }
        public ObservableCollection<string> OpcionesPregunta { get; set; }
        public bool ActivarBotones { get; set; } = false;
        public int TiempoRestantePregunta { get; set; }
        private DispatcherTimer? timerPregunta;

        public ICommand EnviarRespuestaCommand { get; set; }
        public ICommand SeleccionarRespuestaCommand { get; set; }

        public QuizzViewModel(QuizzServer quizzService)
        {

            OpcionesPregunta = new();
            PreguntaActual = new();
            this.quizzService = quizzService;

            quizzService.PreguntaRecibida += QuizzService_PreguntaRecibida;
            quizzService.ResultadosParcialesRecibidos += QuizzService_ResultadosParcialesRecibidos;
            quizzService.NotificacionRecibida += QuizzService_NotificacionRecibida;

            EnviarRespuestaCommand = new RelayCommand<int>(EnviarRespuesta);
            SeleccionarRespuestaCommand = new RelayCommand<object>(SeleccionarRespuesta);
           

            timerPregunta = new DispatcherTimer();
            timerPregunta.Interval = TimeSpan.FromSeconds(1);
            timerPregunta.Tick += TimerPregunta_Tick;

            lecturaTimer = new System.Timers.Timer();
            lecturaTimer.AutoReset = false;
            lecturaTimer.Elapsed += LecturaTimer_Elapsed; ;
        }

        private void SeleccionarRespuesta(object indice)
        {
            try
            {
                if (indice is string str && int.TryParse(str, out int opcion))
                {
                    OpcionSeleccionada = opcion;
                }
                ActualizarDatos();
            }
            catch (Exception)
            {
                MessageBox.Show("Error al seleccionar la respuesta. Por favor, inténtelo de nuevo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LecturaTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ActivarBotones = true;
                timerPregunta?.Start();
                ActualizarDatos();
            });
        }

        private void QuizzService_NotificacionRecibida(object? sender, string e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e == "JuegoFinalizado")
                {
                    timerPregunta.Stop();
                    ActivarBotones = false;
                }
                else if(e== "ReiniciarJuego")
                {
                    Aciertos = 0;
                    PreguntaActual = new();
                    Mensaje = string.Empty;
                    TiempoRestantePregunta = 0;
                    OpcionesPregunta.Clear();
                }
                else if (e == "TiempoPreguntaFinalizado")
                {
                    quizzService.EnviarRespuesta(OpcionSeleccionada);
                }
                ActualizarDatos();
            });
        }

        private void QuizzService_ResultadosParcialesRecibidos(object? sender, ResultadosParcialesDTO e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                timerPregunta.Stop();
                ActivarBotones = false;
                if (!string.IsNullOrEmpty(quizzService?.nombreUsuario) && e.AcertaronUsuarios.Contains(quizzService.nombreUsuario))
                {
                    Aciertos++;
                }
            });
        }

        private void TimerPregunta_Tick(object? sender, EventArgs e)
        {
            TiempoRestantePregunta--;
            if (TiempoRestantePregunta <= 0)
            {
                timerPregunta.Stop();
                Mensaje = "¡Se acabó el tiempo! Tu respuesta seleccionada será enviada.";
                ActivarBotones = false;
            }
            ActualizarDatos();
        }

        private void QuizzService_PreguntaRecibida(object? sender, PreguntaDTO e)
        {
            Mensaje = string.Empty;
            Application.Current.Dispatcher.Invoke(() =>
            {
                PreguntaActual = e;
                OpcionesPregunta.Clear();
                OpcionSeleccionada = -1;
                ActivarBotones = false;
                foreach (var item in e.Opciones)
                {
                    OpcionesPregunta.Add(item);
                }

                // Calcular cuántos segundos han pasado desde que el servidor creó la pregunta
                double segundosTranscurridos = (DateTime.UtcNow - e.HoraInicioUTC).TotalSeconds;

                // Mantener el tiempo de lectura de 5 segundos
                double lectura = 5;

                if (segundosTranscurridos < lectura)
                {
                    // Todavía estamos en el tiempo de lectura
                    double restanteDeLectura = lectura - segundosTranscurridos;

                    // Configurar el temporizador de lectura con el tiempo real restante
                    lecturaTimer.Interval = TimeSpan.FromSeconds(restanteDeLectura).TotalMilliseconds;
                    lecturaTimer.Start();

                    // Cuando termine, inicia el timer real con el tiempo restante ajustado
                    TiempoRestantePregunta = (int)(e.TiempoLimite);
                }
                else
                {
                    // Ya pasó el tiempo de lectura, empezar de inmediato el temporizador
                    TiempoRestantePregunta = (int)(e.TiempoLimite - segundosTranscurridos);

                    if (TiempoRestantePregunta < 0)
                        TiempoRestantePregunta = 0;

                    ActivarBotones = true;
                    timerPregunta?.Start();
                }

                ActualizarDatos();
            });
            

        }

        private void EnviarRespuesta(int respuestaseleccionada)
        {
            try
            {
                OpcionSeleccionada = respuestaseleccionada;
                quizzService.EnviarRespuesta(OpcionSeleccionada);
                ActualizarDatos();
            }
            catch (Exception)
            {
                MessageBox.Show("Error al enviar la respuesta. Por favor, inténtelo de nuevo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void ActualizarDatos(string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
