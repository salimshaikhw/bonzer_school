using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ViewModels.EmailSettings
{
    public class EmailAttachment
    {
        public string FileName { get; set; } = default!;
        public Stream Content { get; set; } = default!;
        public string ContentType { get; set; } = "application/pdf";
    }

}
