﻿namespace KonigLabs.FantaEmotion.CommonViewModels.ViewModels.Factories
{
    public interface IViewModelFactory
    {
        BaseViewModel Get(object param);
    }
}