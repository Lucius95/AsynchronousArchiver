using AsynchronousArchiver.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AsynchronousArchiver.ViewModel
{
    class MainViewModel : INotifyPropertyChanged
    {
        private IDialogService _dialogService;
        private ICommand _openCommand;
        private ICommand _archiving;
        private ICommand _decompressing;
        private string _directory;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        // команда открытия файла
        public ICommand OpenCommand
        {
            get
            {
                return _openCommand ??
                      (_openCommand = new DelegateCommand((obj) =>
                      {
                          try
                          {
                              if (_dialogService.OpenFileDialog() == true)
                              {
                                  //FilePath = _dialogService.FilePath;
                                  AppState.Instance.FilePath = _dialogService.FilePath;
                              }
                          }
                          catch (Exception ex)
                          {

                          }
                      }));
            }
        }

        // команда на архивирования
        public ICommand Archiving
        {
            get
            {
                return _archiving ??
                      (_archiving = new DelegateCommand((obj) =>
                      {
                          try
                          {
                              var archiving = new Archiving();
                              Task.Factory.StartNew(() => archiving.ThreadArchiving());
                          }
                          catch (Exception ex)
                          {

                          }
                      }));
            }
        }

        // команда на разархиварование
        public ICommand Decompressing
        {
            get
            {
                return _decompressing ??
                      (_decompressing = new DelegateCommand((obj) =>
                      {
                          try
                          {
                              var decompress = new Decompress();
                              Task.Factory.StartNew(() => decompress.ThreadDecompress());
                          }
                          catch (Exception ex)
                          {

                          }
                      }));
            }
        }

        public string Status
        {
            get { return AppState.Instance.Status.ToString(); }
            set
            {
                OnPropertyChanged();
            }
        }

        public string TimeProcess
        {
            get { return AppState.Instance.TimeProcess.ToString(@"hh\:mm\:ss"); }
            set
            {
                OnPropertyChanged();
            }
        }

        public byte Progress
        {
            get { return AppState.Instance.Progress; }
            set
            {
                OnPropertyChanged();
            }
        }

        public string Directory
        {
            get { return _directory; }
            set
            {
                _directory = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            _dialogService = new DefaultDialogService();
            AppState.InitAppState();
            StartUpdateGraph();
        }

        public void StartUpdateGraph()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Task.Delay(1000).Wait();
                    Change();
                }
            });
        }

        public void Change()
        {
            Directory = AppState.Instance.Directory;
            TimeProcess = "";
            Status = "";
            Progress = 0;
        }
    }
}
