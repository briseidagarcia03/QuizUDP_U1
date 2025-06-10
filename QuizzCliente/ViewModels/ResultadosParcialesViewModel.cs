using QuizzCliente.Models.DTOs;
using QuizzCliente.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzCliente.ViewModels
{
    public class ResultadosParcialesViewModel:INotifyPropertyChanged
    {
        public string Mensaje { get; set; } = string.Empty;
        public ResultadosParcialesViewModel(ResultadosParcialesDTO resultadosparciales, string nombreusuario)
        {
            if(resultadosparciales.AcertaronUsuarios.Contains(nombreusuario))
            {
                Mensaje = "¡Has acertado la pregunta!";
            }
            else if (resultadosparciales.FallaronUsuarios.Contains(nombreusuario))
            {
                Mensaje = "Has fallado la pregunta.";
            }
            else if (resultadosparciales.NoRespondieronUsuarios.Contains(nombreusuario))
            {
                Mensaje = "No has respondido la pregunta.";
            }
            else
            {
                Mensaje = "No se ha registrado tu respuesta.";
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
