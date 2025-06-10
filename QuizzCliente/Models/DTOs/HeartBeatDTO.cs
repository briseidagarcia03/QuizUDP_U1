using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizzCliente.Models.DTOs
{
    public class HeartBeatDTO:Base
    {
        public string NombreUsuario { get; set; } = null!;
    }
}
