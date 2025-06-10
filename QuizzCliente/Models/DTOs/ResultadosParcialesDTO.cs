using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzCliente.Models.DTOs
{
    public class ResultadosParcialesDTO: Base
    {
        public string TextoPregunta { get; set; } = null!;
        public int IndiceRespuestaCorrecta { get; set; } 
        public List<string>? OpcionesPregunta { get; set; }
        public Dictionary<int, int>? VotosPorOpcion { get; set; }
        public List<string>? AcertaronUsuarios { get; set; }
        public List<string>? FallaronUsuarios { get; set; }
        public List<string>? NoRespondieronUsuarios { get; set; }
    }
}
