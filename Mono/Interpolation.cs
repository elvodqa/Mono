using System.Numerics;

namespace Mono;

public class Interpolation
{
    public static float Map(float input, float input_L, float input_H, float out_L, float out_H) =>
        out_L + ((input - input_L) / (input_H - input_L)) * (out_H - out_L);
    
    public static T Map<T>(T input, T input_L, T input_H, T out_L, T out_H) where T : INumber<T> =>
        out_L + ((input - input_L) / (input_H - input_L)) * (out_H - out_L);
}