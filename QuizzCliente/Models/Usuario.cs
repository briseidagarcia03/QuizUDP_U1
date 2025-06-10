using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuizzCliente.Models
{
    public class Usuario:Base
    {
        public string Nombre { get; set; } = null!;
        public IPEndPoint? RemoteEndPoint { get; set; }
        public DateTime UltimoHeartbeat { get; set; } // Para saber si sigue conectado
        public int Aciertos { get; set; } = 0; // Para llevar la cuenta de aciertos
        public int? RespuestaActual { get; set; } = null; // La última respuesta seleccionada por el usuario
    }
}
