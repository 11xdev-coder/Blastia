namespace Blastia.Main.GameState;

public static class Noise
{
    // God forgive me for what I am about to do.
    
    // lookup table -> randomly arranged array of 0-255 numbers
    private static readonly int[] Permutation =
    [
        151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    ];
    
    private static readonly int[] DoubledPermutation = new int[512];
    
    static Noise()
    {
        for (int i = 0; i < 512; i++)
        {
            DoubledPermutation[i] = Permutation[i % 256];
        }
    }

    private static float Fade(float t)
    {
        // ease curve
        // 6t^5 - 15t^4 + 10t^3
        return t * t * t * (6 * t * t - 15 * t + 10);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private static float Gradient(int hash, float x, float y)
    {
        // Tour to hell and back
        switch (hash & 0xF)
        {
            case 0x0: return x + y;
            case 0x1: return -x + y;
            case 0x2: return x - y;
            case 0x3: return -x - y;
            case 0x4: return y + x;
            case 0x5: return y - x;
            case 0x6: return -y + x;
            case 0x7: return -y - x;
            case 0x8: return x + y;
            case 0x9: return -x + y;
            case 0xA: return x - y;
            case 0xB: return -x - y;
            case 0xC: return y + x;
            case 0xD: return y - x;
            case 0xE: return -y + x;
            case 0xF: return -y - x;
            default: return 0;
        }
    }
     
    public static float Perlin(float x, float y)
    {
        // Ensure positive coordinates
        x = x < 0 ? -x : x;
        y = y < 0 ? -y : y;
        
        // from 0 to 255
        // current point coords
        int xi = (int) x % 255;
        int yi = (int) y % 255;

        // truncate and get only fraction part for smooth fading
        float xFraction = x - (int) x;
        float yFraction = y - (int) y;
        
        // fade values for smooth interpolation
        float xFaded = Fade(xFraction);
        float yFaded = Fade(yFraction);
        
        // gradient values for 4 corners
        var aa = DoubledPermutation[DoubledPermutation[xi] + yi];
        var ab = DoubledPermutation[DoubledPermutation[xi] + yi + 1];
        var ba = DoubledPermutation[DoubledPermutation[xi + 1] + yi];
        var bb = DoubledPermutation[DoubledPermutation[xi + 1] + yi + 1];

        float x1 = Lerp(Gradient(aa, xFraction, yFraction), 
            Gradient(ba, xFraction - 1, yFraction), 
            xFaded);
        
        float x2 = Lerp(Gradient(ab, xFraction, yFraction - 1), 
            Gradient(bb, xFraction - 1, yFraction - 1), 
            xFaded);

        // from [-1, 1] to [0, 1]
        return (Lerp(x1, x2, yFaded) + 1) * 0.5f;
    }

    public static float OctavePerlin(float x, float y, float freq, int octaves, float persistence)
    {
        float total = 0f;
        float amplitude = 1f;
        float maxValue = 0f;  

        for (int i = 0; i < octaves; i++)
        {
            total += Perlin(x * freq, y * freq) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            freq *= 2;
        }

        if (maxValue == 0f) return 0f;
    
        return total / maxValue;
    }
}