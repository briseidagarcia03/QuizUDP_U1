using GalaSoft.MvvmLight.Command;
using QuizzCliente.Services;
using QuizzCliente.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuizzCliente.ViewModels
{
    public class LoginViewModel:INotifyPropertyChanged
    {
        public string IPServidor { get; set; } = "0.0.0.0";
        QuizzServer quizzService;
        public event EventHandler<string>? IpConfirmada;
        public string Mensaje {get;set; }
        public bool Unirse { get; set; } = true;
        public ICommand VerificarIPCommand { get; }


        public LoginViewModel(QuizzServer quizzService)
        {
            this.quizzService = quizzService;

            VerificarIPCommand = new RelayCommand(VerificarIP);
            quizzService.NotificacionRecibida += QuizzService_NotificacionRecibida;
        }

        private void QuizzService_NotificacionRecibida(object? sender, string e)
        {
            if(e == "Pong")
            {
                IpConfirmada?.Invoke(this, IPServidor);
                ActualizarDatos();
            }
            else if(e == "PingTimeout")
            {
                Unirse = true;
                Mensaje = "No se pudo conectar al servidor. Por favor, verifica la dirección IP e intenta nuevamente.";
                ActualizarDatos();
            }
            
        }

        private void VerificarIP()
        {
            if (!string.IsNullOrWhiteSpace(IPServidor) && IPAddress.TryParse(IPServidor, out IPAddress? ipAddress) && IPServidor != "0.0.0.0" && IPServidor != "::0")
            {
                quizzService.EnviarPing(IPServidor);
                Mensaje = "Enviando ping al servidor...";
                Unirse = false;
            }
            else
            {
                Mensaje= "Por favor, ingresa una dirección IP válida.";
            }
            ActualizarDatos();
        }

        private void ActualizarDatos(string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
