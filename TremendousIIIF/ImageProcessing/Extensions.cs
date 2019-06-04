using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace Extensions
{
    /// <summary>
    /// Allow the up to the first eight elements of an array to take part in C# 7's destructuring syntax.
    /// </summary>
    /// <example>
    /// (int first, _, int middle, _, int[] rest) = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    /// var (first, second, rest) = new[] { 1, 2, 3, 4 };
    /// </example>
    public static class ArrayDeconstructionExtensions
    {
        public static void Deconstruct<T>(this T[] array, out T first, out T[] rest)
        {
            first = array[0];
            rest = GetRestOfArray(array, 1);
        }
        public static void Deconstruct<T>(this T[] array, out T first, out T second, out T[] rest)
        {
            first = array[0];
            second = array[1];
            rest = GetRestOfArray(array, 2);
        }
        public static void Deconstruct<T>(this T[] array, out T first, out T second, out T third, out T[] rest)
        {
            first = array[0];
            second = array[1];
            third = array[2];
            rest = GetRestOfArray(array, 3);
        }
        public static void Deconstruct<T>(this T[] array, out T first, out T second, out T third, out T fourth, out T[] rest)
        {
            first = array[0];
            second = array[1];
            third = array[2];
            fourth = array[3];
            rest = GetRestOfArray(array, 4);
        }
        public static void Deconstruct<T>(this T[] array, out T first, out T second, out T third, out T fourth, out T fifth, out T[] rest)
        {
            first = array[0];
            second = array[1];
            third = array[2];
            fourth = array[3];
            fifth = array[4];
            rest = GetRestOfArray(array, 5);
        }
        public static void Deconstruct<T>(this T[] array, out T first, out T second, out T third, out T fourth, out T fifth, out T sixth, out T[] rest)
        {
            first = array[0];
            second = array[1];
            third = array[2];
            fourth = array[3];
            fifth = array[4];
            sixth = array[5];
            rest = GetRestOfArray(array, 6);
        }
        public static void Deconstruct<T>(this T[] array, out T first, out T second, out T third, out T fourth, out T fifth, out T sixth, out T seventh, out T[] rest)
        {
            first = array[0];
            second = array[1];
            third = array[2];
            fourth = array[3];
            fifth = array[4];
            sixth = array[5];
            seventh = array[6];
            rest = GetRestOfArray(array, 7);
        }
        public static void Deconstruct<T>(this T[] array, out T first, out T second, out T third, out T fourth, out T fifth, out T sixth, out T seventh, out T eighth, out T[] rest)
        {
            first = array[0];
            second = array[1];
            third = array[2];
            fourth = array[3];
            fifth = array[4];
            sixth = array[5];
            seventh = array[6];
            eighth = array[7];
            rest = GetRestOfArray(array, 8);
        }
        private static T[] GetRestOfArray<T>(T[] array, int skip)
        {
            return array.Skip(skip).ToArray();
        }
    }
}

