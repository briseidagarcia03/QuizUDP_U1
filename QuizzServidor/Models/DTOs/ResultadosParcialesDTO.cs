using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzServidor.Models.DTOs
{
    public class ResultadosParcialesDTO:Base
    {
        public string TextoPregunta { get; set; } = null!; // Texto de la pregunta
        public int IndiceRespuestaCorrecta { get; set; } // Índice de la respuesta correcta de la pregunta
        public List<string>? OpcionesPregunta { get; set; } // Las opciones de la pregunta
        public Dictionary<int, int>? VotosPorOpcion { get; set; } // Clave: índice de opción, Valor: cantidad de votos
        public List<string>? AcertaronUsuarios { get; set; } // Nombres de usuarios que acertaron
        public List<string>? FallaronUsuarios { get; set; } // Nombres de usuarios que fallaron
        public List<string>? NoRespondieronUsuarios { get; set; } // Nombres de usuarios que no respondieron
    }
}
