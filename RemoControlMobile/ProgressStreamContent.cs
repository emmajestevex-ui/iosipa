using System.Net;

namespace RemoControlMobile;

public class ProgressStreamContent : HttpContent
{
    private readonly Stream stream;

    private readonly int bufferSize;

    private readonly Action<long, long>
        progreso;


    public ProgressStreamContent(
        Stream stream,
        Action<long, long> progreso,
        int bufferSize = 64 * 1024)
    {
        this.stream =
            stream;

        this.progreso =
            progreso;

        this.bufferSize =
            bufferSize;
    }


    protected override bool TryComputeLength(
        out long length)
    {
        if (stream.CanSeek)
        {
            length =
                stream.Length;

            return true;
        }

        length =
            -1;

        return false;
    }


    protected override async Task SerializeToStreamAsync(
        Stream destino,
        TransportContext? context)
    {
        byte[] buffer =
            new byte[
                bufferSize];

        long total =
            stream.CanSeek
                ? stream.Length
                : -1;

        long enviados =
            0;

        int leidos;

        while (
            (leidos =
                await stream.ReadAsync(
                    buffer,
                    0,
                    buffer.Length)) > 0)
        {
            await destino.WriteAsync(
                buffer,
                0,
                leidos);

            enviados +=
                leidos;

            progreso(
                enviados,
                total);
        }
    }


    protected override void Dispose(
        bool disposing)
    {
        if (disposing)
        {
            stream.Dispose();
        }

        base.Dispose(
            disposing);
    }
}