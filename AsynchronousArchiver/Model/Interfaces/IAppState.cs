using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.Model.Interfaces
{
    interface IAppState : IDisposable
    {
        IСompressDecompress CompressDecompress { get; }

        /// <summary>
        /// Имя процесса (Архивация/разархивация)
        /// </summary>
        string NameAction { get; }

        /// <summary>
        /// Статус текущего процесса
        /// </summary>
        StatusOperation Status { get; }

        /// <summary>
        /// Прогресс в процентах текущего процесса
        /// </summary>
        byte Progress { get; }

        /// <summary>
        /// Время текущего процесса
        /// </summary>
        TimeSpan TimeProcess { get; }
    }
}
