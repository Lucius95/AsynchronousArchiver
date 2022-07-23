using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.ViewModel
{
    public interface IDialogService
    {
        string[] FilePath { get; }
        bool OpenFileDialog();  // открытие файла
    }
}
