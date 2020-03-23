﻿namespace NETworkManager.Models
{
    /// <summary>
    /// Class to store informations about an application.
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        /// Name of the application.
        /// </summary>
        public Application.Name Name { get; set; }

        /// <summary>
        /// Indicates that the application is visible to the user.
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Create an empty <see cref="ApplicationInfo"/>.
        /// </summary>
        public ApplicationInfo()
        {

        }

        /// <summary>
        /// Create an <see cref="ApplicationInfo"/> based on <see cref="Application.Name"/>.
        /// </summary>
        /// <param name="name"><see cref="Application.Name"/></param>
        public ApplicationInfo(Application.Name name)
        {
            Name = name;
            IsVisible = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Equals(ApplicationInfo info)
        {
            if (info == null)
                return false;

            return Name == info.Name;
        }

        /// <summary>
        /// Method to check if an object is equal to this object.
        /// </summary>
        /// <param name="obj">Object from type <see cref="ApplicationInfo" />.</param>
        /// <returns>Equality as <see cref="bool"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals(obj as ApplicationInfo);
        }

        /// <summary>
        /// Method to return the hash code of this object.
        /// </summary>
        /// <returns>Hashcode as <see cref="int"/>.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
