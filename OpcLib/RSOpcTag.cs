namespace OpcLib
{
    /// <summary>
    /// Rocwell Automation format Opc Tag
    /// </summary>
    public class RSOpcTag : OpcTag, IOpcTag
    {
        public const string RSOpcItemTemplate = "[{0}]{1}";

        public string Topic { get; set; }

        public RSOpcTag(string topic, string tagName) : base(tagName)
        {
            Topic = topic;
        }

        public new string Address
        {
            get { return string.IsNullOrEmpty(Topic) ? 
                    TagName : 
                    string.Format(RSOpcItemTemplate, Topic, TagName); }
        }
    }
}