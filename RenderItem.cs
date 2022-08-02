using System;

namespace RenderTableCreator
{
    internal class RenderItem : IComparable<RenderItem>
    {
        public string ImageName { get; }
        public string Description { get; }
        public int LineNumber { get; }
        public int RefCount { get; set; } // keeps track of the number of instances 
        

        public RenderItem(string imageName, string description, int lineNumber)
        {
            ImageName = imageName;
            Description = description;
            LineNumber = lineNumber;
            RefCount = 1; 
        }

        public int CompareTo(RenderItem other)
        {
            int result = 0;
            int padCount;
            string thisName = ImageName;
            string otherName = other.ImageName;

            // Normalize lengths of string
            if (thisName.Length < otherName.Length)
            {
                padCount = otherName.Length - thisName.Length;
                thisName = thisName.PadRight(thisName.Length + padCount);
            }
            else if (thisName.Length > otherName.Length)
            {
                padCount = thisName.Length - otherName.Length;
                otherName = otherName.PadRight(otherName.Length + padCount);
            }


            for (int i = 0; i < thisName.Length; i++)
            {
                //result = CompareChar(thisName[i], otherName[i]);
                result = CompareChar(ref i, thisName, otherName);

                if (result == 0)
                {
                    continue;
                }

                return result;

            }

            return result;
        }

        private static int CompareNumbers(ref int index, string thisName, string otherName)
        {
            string thisNumString = string.Empty;
            string otherNumString = string.Empty;
            int idx;

            for(idx = index; idx < thisName.Length; idx++)
            {
                bool thisIsDigit = char.IsDigit(thisName[idx]);
                bool otherIsDigit = char.IsDigit(otherName[idx]);

                if (thisIsDigit)
                    thisNumString += thisName[idx];

                if (otherIsDigit)
                    otherNumString += otherName[idx];

                if (!thisIsDigit && !otherIsDigit)
                    break; 
            }

            int.TryParse(thisNumString, out int thisNumber);
            int.TryParse(otherNumString, out int otherNumber);

            index = idx - 1;

            if (thisNumber < otherNumber)
                return -1;
            if (thisNumber > otherNumber)
                return 1;
            return 0;

        }

        private static int CompareChar(ref int index, string thisName, string otherName)
        {
            // priority
            // space ' '
            // underscore '_' 
            // 0-9 [numbers are sorted by numeric value, not string value]
            // a-z [letters are sorted as normal]                      
            //
            // Critical Errors return -255 
            
            char f = thisName.ToLower()[index];
            char s = otherName.ToLower()[index];

            // Space
            if ((f == ' ') && (s == ' '))
                return 0;
            if ((f == ' ') && (s != ' '))
                return -1;
            if (((f != ' ') && (s == ' ')))
                return 1;
            
            // Underscore 
            if ((f == '_') && (s == '_'))
                return 0;
            if ((f == '_') && (s != '_'))
                return -1;
            if (((f != '_') && (s == '_')))
                return 1;

            // Numbers            
            if (char.IsDigit(f) || char.IsDigit(s))
                return CompareNumbers(ref index, thisName, otherName);


            // Neither inputs are a digit; check for letters (lower chase)
            bool firstIsLetter = char.IsLetter(f);
            bool secondIsLetter = char.IsLetter(s);

            if (firstIsLetter && secondIsLetter)
            {
                if (f < s)
                    return -1;
                if (f > s)
                    return 1;
                return 0;
            }

            // Neither inputs are underscores, spaces, digits or letters 
            // Let the natural char sorting work its magic.            
            if (f < s)
                return -1;
            if (f > s)
                return 1;
            return 0;

        }
    }
}
