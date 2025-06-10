using GalaSoft.MvvmLight.Command;
using QuizzCliente.Models;
using QuizzCliente.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace QuizzCliente.ViewModels
{
    public class EsperaViewModel : INotifyPropertyChanged
    {
        private QuizzServer? quizzService;
        public event EventHandler<(string nombre, string ip)>? UsuarioAceptado;

        public string NombreUsuario { get; set; } = string.Empty;
        public string IPServidor { get; set; } = string.Empty;
        public string Mensaje { get; set; } = "";

        public bool Unirse { get; set; } = true;

        public ICommand VerificarNombreCommand { get; }

        public EsperaViewModel(QuizzServer quizzService, string ip)
        {
            this.quizzService = quizzService;
            this.IPServidor = ip;


            quizzService.ValidacionNombreRecibida += QuizzServer_ValidacionNombreRecibida1;
            quizzService.NotificacionRecibida += QuizzService_NotificacionRecibida;

            VerificarNombreCommand = new RelayCommand(VerificarNombre);
        }

        private void QuizzService_NotificacionRecibida(object? sender, string e)
        {
            if (e == "IniciarJuego" || e == "ReiniciarJuego")
            {
                Unirse = true;
                Mensaje = "";
                NombreUsuario = string.Empty;
                ActualizarDatos();
            }
        }

        private void QuizzServer_ValidacionNombreRecibida1(object? sender, string e)
        {
            if (e == "Duplicado")
            {
                Mensaje = "El nombre de usuario ya está en uso. Por favor, elige otro.";
                NombreUsuario = string.Empty;
                Unirse = true;
            }
            else if (e == "Aceptado")
            {
                Unirse = false;
                Mensaje = "Te has conectado con éxito. El juego comenzara en un momento..";
            }
            ActualizarDatos();

        }

        private void VerificarNombre()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NombreUsuario))
                {
                    Mensaje = "Ingresa un nombre válido.";
                    ActualizarDatos();
                    return;
                }
                quizzService.EstablecerNombre(NombreUsuario);
                quizzService.EnviarUsuarioConectado();
            }
            catch (Exception)
            {
               Mensaje = "Error al establecer el nombre. Por favor, intenta de nuevo.";
                ActualizarDatos();
            }
        }


        private void ActualizarDatos(string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
