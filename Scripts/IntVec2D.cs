using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// had to do the .Equals and .GetHashCode stuff so that dictionaries with IntVec2D keys would work properly
// learned I needed to do that from https://stackoverflow.com/questions/13262106/dictionary-containskey-how-does-it-work
public class IntVec2D: IEquatable<IntVec2D>
{
    public int x;
    public int y;

    public IntVec2D(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public IntVec2D()
    {
        x = 0;
        y = 0;
    }

    public static IntVec2D operator +(IntVec2D a, IntVec2D b)
    {
        return new IntVec2D(a.x + b.x, a.y + b.y);
    }

    public bool Equals(IntVec2D other)
    {
        return ((other.x == x) && (other.y == y));
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;

        if (obj.GetType() != this.GetType()) return false;

        return Equals((IntVec2D)obj);
    }

    public override int GetHashCode()
    {
        return 31 * (31 + x*(2<<31)) + y*(2<<31);
    }
}
