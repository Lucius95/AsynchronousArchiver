using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.ViewModel
{
    class DefaultDialogService : IDialogService
    {
        private string[] _filePath;
        public string[] FilePath
        {
            get { return _filePath; }
        }

        public bool OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                _filePath = openFileDialog.FileNames;
                return true;
            }
            return false;
        }
    }
}
