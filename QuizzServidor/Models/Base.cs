using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzServidor.Models
{
    public class Base
    {
        public string Tipo { get; set; } = null!; // Tipo de mensaje (Pregunta, Respuesta, Usuario)
    }
}
