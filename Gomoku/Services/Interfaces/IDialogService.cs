using Gomoku.Services.Wpf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gomoku.Services.Interfaces
{
    public interface IDialogService
    {
        Task<T?> ShowAsync<T>(T vm, DialogSection section = DialogSection.Main)
            where T : class, IDialogViewModel;
    }
}
