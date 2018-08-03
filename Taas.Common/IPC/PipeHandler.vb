
Imports System.IO.Pipes

Namespace IPC
    Public Class PipeHandler

        Public Event PipeData As PipeDataEvent

        Public ReadOnly Property PipeIn As PipeStream
        Public ReadOnly Property PipeOut As PipeStream
        Private ReadOnly _buffer_size As Integer
        Private _data_length As Integer = -1
        Private _data_buffer As Byte()

        Public Sub New(pipeIn As PipeStream, pipeOut As PipeStream, Optional bufferSize As Integer = 1024)
            Me.PipeIn = pipeIn
            Me.PipeOut = pipeOut
            Me._buffer_size = bufferSize
        End Sub

        Public Sub Run()
            Dim buffer As Byte() : ReDim buffer(_buffer_size - 1)
            Dim length As Integer = 0
            While True
                length = Me.PipeIn.Read(buffer, 0, _buffer_size)
                If Not length = 0 Then
                    If Me._data_length = -1 Then
                        Dim total As Integer = buffer(0) Or (buffer(1) * &H100)
                        ReDim Me._data_buffer(total - 1)
                        Me._data_length = 0
                    Else
                        Array.Copy(buffer, 0, Me._data_buffer, Me._data_length, length)
                        Me._data_length += length
                        If Me._data_length = Me._data_buffer.Count Then
                            Me._data_length = -1
                            RaiseEvent PipeData(Me, New PipeDataEventArgs(Me._data_buffer))
                        End If
                    End If
                Else
                    Exit While
                End If
            End While
        End Sub

        Public Sub SendMessage(buffer As Byte())
            Dim current As Integer = 0
            Dim total As Integer = Math.Ceiling(buffer.Count / Me._buffer_size)
            Me.PipeOut.Write(New Byte() {buffer.Count And &HFF, (buffer.Count >> 8) And &HFF}, 0, 2)
            Do
                Dim size As Integer = Math.Min(Me._buffer_size, buffer.Count - current)
                Me.PipeOut.Write(buffer, current, size)
                Me.PipeOut.Flush()
                Me.PipeOut.WaitForPipeDrain()
                current += size
            Loop Until current = buffer.Count
        End Sub

        Public Sub SendMessageSafe(buffer As Byte(), Optional timeout As Double = 1000)
            Dim remote_ex As Exception = Nothing
            Dim thr As New Threading.Thread(
                Sub()
                    Try : Me.SendMessage(buffer)
                    Catch thex As Threading.ThreadAbortException
                    Catch ex As Exception
                        remote_ex = ex
                    End Try
                End Sub
            )
            thr.Start()
            If Not Me._await_event(Function() Not thr.IsAlive, timeout) Then
                thr.Abort()
                Throw New Exception("Unable to execute the request: Operation timed out.")
            End If
            If remote_ex IsNot Nothing Then Throw New Exception("An error occured during the remote operation.", remote_ex)
        End Sub

        Private Function _await_event(predicate As Func(Of Boolean), timeout As Double) As Boolean
            Dim retval As Boolean = True
            Dim sw As New Stopwatch
            sw.Start()
            While Not predicate()
                If sw.ElapsedMilliseconds > timeout Then
                    retval = False
                    Exit While
                End If
                Threading.Thread.Sleep(1)
            End While
            sw.Stop()
            Return retval
        End Function

        Public Sub Close()
            Me.PipeIn.Close()
            Me.PipeOut.Close()
        End Sub

    End Class

End Namespace
