﻿namespace CryptoMailClient.Utilities
{
    public interface IDeepCloneable
    {
        object DeepClone();
    }

    public interface IDeepCloneable<out T> : IDeepCloneable
    {
        new T DeepClone();
    }
}
