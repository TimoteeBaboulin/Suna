/*******************************************************************************
The content of this file includes portions of the proprietary AUDIOKINETIC Wwise
Technology released in source code form as part of the game integration package.
The content of this file may not be used without valid licenses to the
AUDIOKINETIC Wwise Technology.
Note that the use of the game engine is subject to the Unity(R) Terms of
Service at https://unity3d.com/legal/terms-of-service
 
License Usage
 
Licensees holding valid licenses to the AUDIOKINETIC Wwise Technology may use
this file in accordance with the end user license agreement provided with the
software or, alternatively, in accordance with the terms contained
in a written agreement between you and Audiokinetic Inc.
Copyright (c) 2024 Audiokinetic Inc.
*******************************************************************************/
#if UNITY_EDITOR_OSX || (UNITY_STANDALONE_OSX && !UNITY_EDITOR)
using System.Threading.Tasks;

public partial class WwiseProjectDatabase
{
    public static void PostInitCallback()
    {
        SoundBankDirectoryUpdated?.Invoke();
    }
    public static event System.Action SoundBankDirectoryUpdated;
    public static async Task<bool> InitAsync(string inDirectoryPath, string inDirectoryPlatformName)
    {
       InitCheckUp(inDirectoryPath);
       if (!ProjectInfoExists)
           return false;
               
       await Task.Run(() => WwiseProjectDatabasePINVOKE_Mac.Init(inDirectoryPath, inDirectoryPlatformName));
       return ProjectInfoExists;
    }

    public static void Init(string inDirectoryPath, string inDirectoryPlatformName, string language)
    {
        InitCheckUp(inDirectoryPath);
        if (!ProjectInfoExists)
            return;
        
        WwiseProjectDatabasePINVOKE_Mac.Init(inDirectoryPath, inDirectoryPlatformName);
        WwiseProjectDatabasePINVOKE_Mac.SetCurrentLanguage(language);
    }

    public static void SetCurrentPlatform(string inDirectoryPlatformName)
    {
        WwiseProjectDatabasePINVOKE_Mac.SetCurrentPlatform(inDirectoryPlatformName);
    }

    public static void SetCurrentLanguage(string inLanguageName)
    {
        WwiseProjectDatabasePINVOKE_Mac.SetCurrentLanguage(inLanguageName);
    }

    public static string StringFromIntPtrString(System.IntPtr ptr)
    {
        return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
    }
    
    /*
     * SoundBanks
     */

    public static global::System.IntPtr GetSoundBankRefString(string soundBankName, string soundBankType)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankRefString(soundBankName, soundBankType);
    }
    public static global::System.IntPtr GetAllSoundBanksRef()
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetAllSoundBanksRef();
    }
    public static uint GetSoundBankCount()
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankCount();
    }
    public static global::System.IntPtr GetSoundBankRefIndex(global::System.IntPtr soundBankArrayRef, int index)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankRefIndex(soundBankArrayRef, index);
    }
    public static void DeleteSoundBanksArrayRef(global::System.IntPtr soundBankArrayRef)
    {
        WwiseProjectDatabasePINVOKE_Mac.DeleteSoundBanksArrayRef(soundBankArrayRef);
    }
    public static string GetSoundBankName(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetSoundBankName(soundBankRefPtr));
    }
    public static string GetSoundBankPath(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetSoundBankPath(soundBankRefPtr));
    }
    public static string GetSoundBankLanguage(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetSoundBankLanguage(soundBankRefPtr));
    }
    public static uint GetSoundBankLanguageId(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankLanguageId(soundBankRefPtr);
    }
    public static System.IntPtr GetSoundBankGuid(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankGuid(soundBankRefPtr);
    }
    public static uint GetSoundBankShortId(global::System.IntPtr soundBankRefPtr)
    {
       return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankShortId(soundBankRefPtr);
    }
    public static bool IsUserBank(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.IsUserBank(soundBankRefPtr);
    }
    public static bool IsInitBank(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.IsInitBank(soundBankRefPtr);
    }
    public static bool IsSoundBankValid(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.IsSoundBankValid(soundBankRefPtr);
    }

    public static global::System.IntPtr GetSoundBankMedia(global::System.IntPtr soundBankRefPtr, int index)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankMedia(soundBankRefPtr, index);
    }

    public static uint GetSoundBankMediasCount(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankMediasCount(soundBankRefPtr);
    }

    public static global::System.IntPtr GetSoundBankEvent(global::System.IntPtr soundBankRefPtr, int index)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankEvent(soundBankRefPtr, index);
    }

    public static uint GetSoundBankEventsCount(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetSoundBankEventsCount(soundBankRefPtr);
    }

    public static void DeleteSoundBankRef(global::System.IntPtr soundBankRefPtr)
    {
        WwiseProjectDatabasePINVOKE_Mac.DeleteSoundBankRef(soundBankRefPtr);
    }
    
    /*
     * Medias
    */
    
    public static string GetMediaName(global::System.IntPtr mediaRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetMediaName(mediaRefPtr));
    }
    public static string GetMediaPath(global::System.IntPtr mediaRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetMediaPath(mediaRefPtr));
    }
    public static uint GetMediaShortId(global::System.IntPtr mediaRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetMediaShortId(mediaRefPtr);
    }
    public static string GetMediaLanguage(global::System.IntPtr mediaRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetMediaLanguage(mediaRefPtr));
    }
    public static bool GetMediaIsStreaming(global::System.IntPtr mediaRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetMediaIsStreaming(mediaRefPtr);
    }
    public static uint GetMediaLocation(global::System.IntPtr mediaRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetMediaLocation(mediaRefPtr);
    }
    public static string GetMediaCachePath(global::System.IntPtr mediaRefPtr)
    {
       return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetMediaCachePath(mediaRefPtr));
    }
    public static void DeleteMediaRef(global::System.IntPtr mediaRefPtr)
    {
        WwiseProjectDatabasePINVOKE_Mac.DeleteMediaRef(mediaRefPtr);
    }
    
    /*
     * Events
    */
    
    public static global::System.IntPtr GetEventRefString(string soundBankName)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventRefString(soundBankName);
    }
    public static string GetEventName(global::System.IntPtr eventRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetEventName(eventRefPtr));
    }
    public static string GetEventPath(global::System.IntPtr eventRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetEventPath(eventRefPtr));
    }
    public static System.IntPtr GetEventGuid(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventGuid(soundBankRefPtr);
    }
    public static uint GetEventShortId(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventShortId(soundBankRefPtr);
    }
    public static float GetEventMaxAttenuation(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventMaxAttenuation(soundBankRefPtr);
    }
    public static float GetEventMinDuration(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventMinDuration(soundBankRefPtr);
    }
    public static float GetEventMaxDuration(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventMaxDuration(soundBankRefPtr);
    }
    public static global::System.IntPtr GetEventMedia(global::System.IntPtr soundBankRefPtr, int index)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventMedia(soundBankRefPtr, index);
    }    
    public static uint GetEventMediasCount(global::System.IntPtr soundBankRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetEventMediasCount(soundBankRefPtr);
    }
    public static void DeleteEventRef(global::System.IntPtr eventRefPtr)
    {
        WwiseProjectDatabasePINVOKE_Mac.DeleteEventRef(eventRefPtr);
    }
    
    /*
     * Platform
    */
    
    public static global::System.IntPtr GetPlatformRef(string soundBankName)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetPlatformRef(soundBankName);
    }
    public static string GetPlatformName(global::System.IntPtr platformRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetPlatformName(platformRefPtr));
    }
    public static global::System.IntPtr GetPlatformGuid(global::System.IntPtr platformRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetPlatformGuid(platformRefPtr);
    }
    public static void DeletePlatformRef(global::System.IntPtr platformRefPtr)
    {
        WwiseProjectDatabasePINVOKE_Mac.DeletePlatformRef(platformRefPtr);
    }

    /**
    * Plugin
    */

    public static global::System.IntPtr GetAllPluginRef()
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetAllPluginRef();
    }
    public static uint GetPluginCount()
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetPluginCount();
    }
    public static global::System.IntPtr GetPluginRefIndex(global::System.IntPtr soundBankArrayRef, int index)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetPluginRefIndex(soundBankArrayRef, index);
    }
    public static uint GetPluginId(global::System.IntPtr pluginRefPtr)
    {
        return WwiseProjectDatabasePINVOKE_Mac.GetPluginId(pluginRefPtr);
    }
    public static string GetPluginName(global::System.IntPtr pluginRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetPluginName(pluginRefPtr));
    }
    public static string GetPluginDLL(global::System.IntPtr pluginRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetPluginDLL(pluginRefPtr));
    }
    public static string GetPluginStaticLib(global::System.IntPtr pluginRefPtr)
    {
        return WwiseProjectDatabase.StringFromIntPtrString(WwiseProjectDatabasePINVOKE_Mac.GetPluginStaticLib(pluginRefPtr));
    }
    public static void DeletePluginRef(global::System.IntPtr pluginRefPtr)
    {
        WwiseProjectDatabasePINVOKE_Mac.DeletePluginRef(pluginRefPtr);
    }
    public static void DeletePluginArrayRef(global::System.IntPtr pluginArrayRefPtr)
    {
        WwiseProjectDatabasePINVOKE_Mac.DeletePluginArrayRef(pluginArrayRefPtr);
    }
}
#endif