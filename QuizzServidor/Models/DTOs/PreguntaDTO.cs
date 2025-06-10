using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzServidor.Models.DTOs
{
    public class PreguntaDTO:Base
    {
        public string Texto { get; set; } = null!;
        public List<string> Opciones { get; set; } = new List<string>();
        public double TiempoLimite { get; set; } // Tiempo límite en segundos para responder

        public int RespuestaCorrecta { get; set; } // Índice de la respuesta correcta en Opciones

        public DateTime HoraInicioUTC { get; set; }

    }
}
