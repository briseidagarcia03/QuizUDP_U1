using QuizzCliente.Services;
using QuizzCliente.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QuizzCliente.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public object VistaActual { get; set; }
        private LoginViewModel loginVM;
        private EsperaViewModel esperaVM;
        private QuizzViewModel quizzVM;
        private QuizzServer quizzService;
        private ResultadosParcialesViewModel parcialesVM;
        private ResultadosFinalesViewModel finalesVM;

        public string NombreUsuarioActual { get; private set; } = string.Empty;


        public MainViewModel()
        {
            quizzService = new QuizzServer("0.0.0.0");

            loginVM = new LoginViewModel(quizzService);
            loginVM.IpConfirmada += LoginVM_IpConfirmada;
            quizzService.PreguntaRecibida += QuizzService_PreguntaRecibida;
            quizzService.ResultadosParcialesRecibidos += QuizzService_ResultadosParcialesRecibidos;
            quizzService.NotificacionRecibida += QuizzService_NotificacionRecibida;
            quizzService.ValidacionNombreRecibida += QuizzService_ValidacionNombreRecibida;

            // Mostrar la primera vista
            VistaActual = loginVM;
        }

        private void QuizzService_ValidacionNombreRecibida(object? sender, string e)
        {
            if (e == "Aceptado")
            {
                NombreUsuarioActual = quizzService.nombreUsuario;
                ActualizarDatos();
            }
        }

        private void QuizzService_ResultadosParcialesRecibidos(object? sender, Models.DTOs.ResultadosParcialesDTO e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                parcialesVM = new ResultadosParcialesViewModel(e, NombreUsuarioActual);
                VistaActual = parcialesVM;
            });
            ActualizarDatos();
        }

        private void QuizzService_NotificacionRecibida(object? sender, string e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e == "IniciarJuego")
                {
                    if (quizzVM == null)
                    {
                        quizzVM = new QuizzViewModel(quizzService);
                    }
                    VistaActual = quizzVM;
                }
                else if (e == "ResultadosFinales")
                {
                    // Cuando el juego termina, mostrar la vista de resultados finales
                    finalesVM = new ResultadosFinalesViewModel(quizzVM?.Aciertos ?? 0); // Usar aciertos del quizzVM
                    VistaActual = finalesVM;
                }
                else if (e == "ReiniciarJuego")
                {
                    VistaActual = esperaVM;
                    ActualizarDatos();
                }
                    ActualizarDatos();
            });
        }

        private void QuizzService_PreguntaRecibida(object? sender, Models.DTOs.PreguntaDTO e)
        {
            if (!(VistaActual is QuizzViewModel))
            {
                if (quizzVM == null)
                {
                    quizzVM = new QuizzViewModel(quizzService);
                }
                VistaActual = quizzVM;
                ActualizarDatos();
            }
        }

        private void LoginVM_IpConfirmada(object? sender, string ip)
        {
            esperaVM = new EsperaViewModel(quizzService,ip);
            VistaActual = esperaVM;
            ActualizarDatos();
        }

        private void ActualizarDatos(string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
