using System.ComponentModel;
using TranslatorGW.Template.Configuration;

namespace TranslatorGW.Configuration;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.

        By default, configuration saves as "Config.json" in mod user config folder.    
        Need more config files/classes? See Configuration.cs

        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */


    [Category("Languages")]
    [DisplayName("English override")]
    [Description("Override the English language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string EnglishOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Korean override")]
    [Description("Override the Korean language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string KoreanOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("French override")]
    [Description("Override the French language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string FrenchOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("German override")]
    [Description("Override the German language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string GermanOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Italian override")]
    [Description("Override the Italian language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string ItalianOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Spanish override")]
    [Description("Override the Spanish language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string SpanishOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Traditional Chinese override")]
    [Description("Override the Traditional Chinese language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string TraditionalChineseOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Japanese override")]
    [Description("Override the Japanese language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string JapaneseOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Polish override")]
    [Description("Override the Polish language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string PolishOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Russian override")]
    [Description("Override the Russian language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string RussianOverride { get; set; } = "";

    [Category("Languages")]
    [DisplayName("Bork! Bork! Bork! override")]
    [Description("Override the BorkBorkBork! language with a custom language. Use the following format: <name>,<file>")]
    [DefaultValue("")]
    public string BorkBorkBorkOverride { get; set; } = "";

}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}