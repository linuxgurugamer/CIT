namespace CIT_Util.Types
{
    public class SettingsEntry
    {
        public object DefaultValue { get; private set; }
        public object Value { get; set; }

        public SettingsEntry(object defaultValue)
        {
            this.DefaultValue = defaultValue;
        }
    }
}