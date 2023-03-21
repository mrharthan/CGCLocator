using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace LRSLocator
{
    [Serializable]
    public class ComReleaser : IDisposable
    {
        // Fields
        private ArrayList _array = ArrayList.Synchronized(new ArrayList());

        // Methods
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            int count = this._array.Count;
            for (int i = 0; i < count; i++)
            {
                if ((this._array[i] != null) && Marshal.IsComObject(this._array[i]))
                {
                    while (Marshal.ReleaseComObject(this._array[i]) > 0)
                    {
                    }
                }
            }

            if (disposing)
            {
                this._array = null;
            }
        }

        ~ComReleaser()
        {
            this.Dispose(false);
        }

        public void ManageLifetime(object o)
        {
            this._array.Add(o);
        }

        public static void ReleaseCOMObject(object o)
        {
            if ((o != null) && Marshal.IsComObject(o))
            {
                while (Marshal.ReleaseComObject(o) > 0)
                {
                }
            }
        }
    }

}
