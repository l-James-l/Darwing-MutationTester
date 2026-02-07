namespace Models.Events;

/// <summary>
/// Event published when a setting is changed.
/// The param should be set using nameof(IMutationSettings.Property)
/// </summary>
public class SettingChanged : PubSubEvent<string>
{

}