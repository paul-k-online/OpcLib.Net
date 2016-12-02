using System;

namespace OpcLib
{
    public interface IOpcTag
    {
        string Address { get; }
        object Value { get; set; }
        string ValueStr { get; }
    }



    /// <summary>
    /// Generic Opc Tag 
    /// </summary>
    public class OpcTag : IOpcTag
    {
        public string TagName { get; set; }
        public object Value { get; set; }

        public string ValueStr
        {
            get
            {
                /*
                if (Value is bool)
                    return Convert.ToBoolean(Value).ToString();
                if (Value is int)
                    return Convert.ToInt32(Value).ToString();
                if (Value is int)
                    return Convert.ToInt32(Value).ToString();
                */

                // return Value == null ? "" : Value.ToString();
                return Value?.ToString() ?? "";
            }
        }

        public OpcTag(string tagName)
        {
            TagName = tagName;
        }

        public string Address
        {
            get { return TagName; }
        }

        public override string ToString()
        {
            return string.Format("\"{0}\" = {1}", Address, ValueStr);
        }
    }

}
