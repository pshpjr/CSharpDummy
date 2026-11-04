namespace CSharpDummy.NetLib;

public class PacketCodec
{
    public static void Encode(Span<byte> data, byte staticKey, byte randomKey)
    {
        byte p = 0;
        byte e = 0;

        for (int k = 0; k < data.Length; k++)
        {
            int i = k + 1;

            p = unchecked((byte)(data[k] ^ (p + randomKey + i)));
            e = unchecked((byte)(p ^ (e + staticKey + i)));

            data[k] = e;
        }
    }

    public static void Decode(Span<byte> first, Span<byte> second, byte staticKey, byte randomKey)
    {
        byte e = 0;
        byte oldP = 0;
        int i = 0;

        DecodeData(first);
        DecodeData(second);

        void DecodeData(Span<byte> data)
        {
            foreach (ref byte value in data)
            {
                i++;

                byte tmp = value;
                byte p = unchecked((byte)(tmp ^ (e + staticKey + i)));

                e = tmp;
                value = unchecked((byte)(p ^ (oldP + randomKey + i)));
                oldP = p;
            }
        }
    }

    public static byte CalculateChecksum(Span<byte> data)
    {
        byte ret = 0;

        foreach (var i in data)
        {
            ret += i;
        }
        return ret;
    }
}
