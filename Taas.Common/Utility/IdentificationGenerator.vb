Imports System.Runtime.CompilerServices

Namespace Utility
    Public Class IdentificationGenerator

        Private Shared ReadOnly _ID_CACHE As New Dictionary(Of Type, Integer)

        Public Shared Function GetNext(Of T)() As Integer
            Dim current As Integer = 0
            Dim key As Type = GetType(T)
            If _ID_CACHE.ContainsKey(key) Then
                current = _ID_CACHE(key)
                _ID_CACHE.Remove(key)
            End If
            _ID_CACHE.Add(key, current + 1)
            Return current
        End Function

    End Class

End Namespace
