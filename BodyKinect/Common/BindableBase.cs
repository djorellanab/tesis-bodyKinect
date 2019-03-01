//------------------------------------------------------------------------------
// <copyright file="BindableBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace BodyKinect.Common
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Simplica el modelo de notificacion
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Evento multicast de notificaciones (Verifica que una propiedad a cambiado)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Comprueba si un valor coincide con otro y notifica al oyente cuando es necesario
        /// </summary>
        /// <typeparam name="T">tipo de propiedad</typeparam>
        /// <param name="storage">Referente a valores de modificacion (Get y set).</param>
        /// <param name="value">Valor deseado.</param>
        /// <param name="propertyName">Nombre de la propiedad a notificar</param>
        /// <returns>verdadero si el valor es cambiado y falso si no lo es</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Notificacion de la propiedad hacambiado
        /// </summary>
        /// <param name="propertyName">Nombre de la propiedad cambiado</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
