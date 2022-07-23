using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsynchronousArchiver.Model.Interfaces
{
    interface IСompressDecompress : IDisposable
    {
        /// <summary>
        /// Статус архивации/разархивации
        /// </summary>
        StatusOperation Status { get; }

        /// <summary>
        /// Прогресс в процентах архивации/разархивации
        /// </summary>
        byte Progress { get; }

        /// <summary>
        /// Время архивации/разархивации
        /// </summary>
        TimeSpan TimeProcess { get; }
    }
}
