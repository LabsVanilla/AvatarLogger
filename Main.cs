using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using WebSocketSharp;

namespace AvatarLogger
{
    public class AvatarLoggerClass : MelonMod
    {
        private static Thread tr = new Thread(connect.Runsocket);
        public override void OnApplicationStart()
        {
            if (File.Exists($"{MelonUtils.GameDirectory}\\Mods\\GalaxyLoader.dll"))
            { MelonLogger.Warning("Galaxy Is Installed using that instead"); return; }
            if (File.Exists($"{MelonUtils.GameDirectory}\\Mods\\GalaxyClient.dll"))
            { MelonLogger.Warning("Galaxy Is Installed using that instead"); return; }
            tr.Start();
            MelonLogger.Msg("Starting Galaxy Avatars Standalone please wait");
            MelonLogger.Msg("Thank You for helping Log Avatars");
            CheckServer.SetTimer();
            Patch.PatchMeth();
        }
    }

    public class Patch
    {
        protected internal static readonly HarmonyInstance Nebula = HarmonyInstance.Create("Nebula Avatars Patch");

        public Patch(Type PatchClass, Type YourClass, string Method, string ReplaceMethod, BindingFlags stat = BindingFlags.Static, BindingFlags pub = BindingFlags.NonPublic)
        { Nebula.Patch(AccessTools.Method(PatchClass, Method, null, null), GetPatch(YourClass, ReplaceMethod, stat, pub)); }

        protected internal HarmonyMethod GetPatch(Type YourClass, string MethodName, BindingFlags stat, BindingFlags pub)
        { return new HarmonyMethod(YourClass.GetMethod(MethodName, stat | pub)); }

        public static HarmonyMethod GetPatch(string name)
        { return new HarmonyMethod(typeof(Patch).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)); }

        [Obsolete]
        public static unsafe void PatchMeth()
        {
            Nebula.Patch(typeof(VRCPlayer).GetMethod(nameof(VRCPlayer.Awake)), null, GetPatch(nameof(AVIPATCHMETHOD)));
            MethodInfo[] methods = typeof(VRCPlayer).GetMethods().Where(mb => mb.Name.StartsWith("Method_Private_Void_GameObject_VRC_AvatarDescriptor_Boolean_")).ToArray();
        }

        protected internal static void AVIPATCHMETHOD(VRCPlayer __instance)
        {
            if (__instance == null) return;
            //__instance.Method_Public_add_Void_MulticastDelegateNPublicSealedVoUnique_0(new Action(() =>

            __instance.Method_Public_add_Void_OnAvatarIsReady_0(new Action(() =>
            {
                if (__instance._player != null && __instance._player.field_Private_APIUser_0 != null && __instance.field_Private_ApiAvatar_0 != null)
                {
                    var p = __instance._player.field_Private_APIUser_0;
                    var a = __instance.field_Private_ApiAvatar_0;

                    var senda = new Logavi()
                    {
                        AvatarName = a.name,
                        Author = a.authorName,
                        Authorid = a.authorId,
                        Avatarid = a.id,
                        Description = a.description,
                        Asseturl = a.assetUrl,
                        Image = a.imageUrl,
                        Platform = a.platform,
                        Status = a.releaseStatus,
                        code = "9",
                    };
                    connect.sendmsg($"{JsonConvert.SerializeObject(senda)}");
                }
            }));
        }
    }

     internal class connect
    {
        internal static WebSocket wss;
        internal static bool waitfortime = false;
        internal protected static bool ath = false;
        internal static string aviserchl = "";
        public static void Runsocket()
        {
            using (wss = new WebSocket("ws://avatarlogger.ws.galaxyvrc.xyz:8080"))
            {
                wss.Connect();
                wss.OnClose += (sender, e) =>
                { tryrecconect(); };
                wss.OnOpen += (sender, e) =>
                {
                    var sendidtosv = new sendsinglemsg()
                    {
                        Custommsg = "Galaxy Avatar Logger LOGIN IGNORE",
                        code = "1",
                    };
                    sendmsg($"{JsonConvert.SerializeObject(sendidtosv)}");
                    MelonLogger.Msg("[Success] Connected to WS API");
                };
                wss.OnMessage += Ws_OnMessage;
                wss.Log.Output = (_, __) => { };
            }
        }

        protected internal static void sendmsg(string text)
        {
            if (connect.wss.IsAlive)
                connect.wss.Send(text);
        }

        protected internal static void tryrecconect()
        {
            try
            {
                if (!connect.wss.IsAlive)
                    wss.Connect();
            }
            catch (Exception error)
            {
                MelonLogger.Error($"[Server]: Cloud not connect : {error}");
                MelonLogger.Error("[Server]: Server Down Possibly");
                wss.Connect();
            }
        }

        protected internal static void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            var message = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(e.Data));

            if (message.ToString() == "Server Error 1")
            { MelonLogger.Error("[SERVER ERROR] Unknown Issue With Server HyperV is working on it dont wory"); }

            if (message.ToString() == "Server Error 2")
            {
                MelonLogger.Error("Server Mantinace", "Server");
                MelonLogger.Error("[SERVER ERROR] Server Mantinace Logging will most likely not work");
            }
            if (message.ToString() == "Close Connection")
            { MelonLogger.Error("[SERVER ERROR] Closed WS why idk ask Hyper"); }

            if (message.Contains("Global"))
            {
                if (Config.GlobalMessage != message)
                {
                    MelonLogger.Log(message);
                    Config.ServerNotify = true;
                    Config.GlobalMessage = message;
                }
            }
        }
    }

     internal class CheckServer
    {
        internal static System.Timers.Timer CHECKSERVER;
        internal static void SetTimer()
        {
            CHECKSERVER = new System.Timers.Timer(15000);
            CHECKSERVER.Elapsed += IsUp;
            CHECKSERVER.AutoReset = true;
            CHECKSERVER.Enabled = true;
            MelonLogger.Log("[SUCCESS] Started Server Heartbeat");
        }

        protected internal static void IsUp(System.Object source, ElapsedEventArgs e)
        {
            var sendidtosv = new sendsinglemsg()
            {
                Custommsg = "",
                code = "20",
            };
            connect.sendmsg($"{JsonConvert.SerializeObject(sendidtosv)}");
        }
    }

    internal class sendsinglemsg
    {
        public string Custommsg { get; set; }
        public string code { get; set; }
    }

    internal class Config
    {
        internal static string GlobalMessage = "";
        internal static bool ServerNotify = false;
    }

    internal class Logavi
    {
        public string AvatarName { get; set; }
        public string Author { get; set; }
        public string Authorid { get; set; }
        public string Avatarid { get; set; }
        public string Description { get; set; }
        public string Asseturl { get; set; }
        public string Image { get; set; }
        public string Platform { get; set; }
        public string Status { get; set; }
        public string code { get; set; }
    }
}





