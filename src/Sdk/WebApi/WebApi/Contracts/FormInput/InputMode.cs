using System.Runtime.Serialization;

namespace GitHub.Services.FormInput
{
    /// <summary>
    /// Mode in which a subscription input should be entered (in a UI)
    /// </summary>
    [DataContract]
    public enum InputMode
    {
        /// <summary>
        /// This input should not be shown in the UI
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// An input text box should be shown
        /// </summary>
        [EnumMember]
        TextBox = 10,

        /// <summary>
        /// An password input box should be shown
        /// </summary>
        [EnumMember]
        PasswordBox = 20,

        /// <summary>
        /// A select/combo control should be shown
        /// </summary>
        [EnumMember]
        Combo = 30,

        /// <summary>
        /// Radio buttons should be shown
        /// </summary>
        [EnumMember]
        RadioButtons = 40,

        /// <summary>
        /// Checkbox should be shown(for true/false values)
        /// </summary>
        [EnumMember]
        CheckBox = 50,

        /// <summary>
        /// A multi-line text area should be shown
        /// </summary>
        [EnumMember]
        TextArea = 60
    }
}
