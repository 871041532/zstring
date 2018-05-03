using System;

public interface IStringBlock : IDisposable
{
    bool Remove(CString str);
}

