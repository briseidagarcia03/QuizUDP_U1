using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzCliente.ViewModels
{
    public class ResultadosFinalesViewModel:INotifyPropertyChanged
    {
        public int AciertosTotales { get; set; }

        public ResultadosFinalesViewModel(int aciertos)
        {
           AciertosTotales = aciertos;
           ActualizarDatos();
        }

        private void ActualizarDatos(string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
