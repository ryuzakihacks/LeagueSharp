#region Credits
//  Special Thanks:
//      xQx - For his Swain, Some of the functions here are from that assembly. 
//      xSalice - For his blitzcrank assembly, I modified one of its function and adapted it to swain.
//      SSJ4 - For his base template
#endregion

#region
using System;
using System.Collections;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using System.Collections.Generic;
using System.Threading;
#endregion

namespace The_Mocking_Swain
{
    class Program
    {
        private const string Champion = "Swain";
        private static Orbwalking.Orbwalker Orbwalker;
        private static Spell Q,W,E,R; 
        private static Menu Config; 
        private static Items.Item Zhonya; 
        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static bool RavenForm;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {

            if (ObjectManager.Player.BaseSkinName != Champion) return;

            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W, 820);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R, 625);

            
            Q.SetTargetted(0.5f, float.MaxValue);
            W.SetSkillshot(0.5f, 275, 1250, false, SkillshotType.SkillshotCircle);
            E.SetTargetted(0.5f, 1400);

            Zhonya = new Items.Item(3157);

            Config = new Menu("The Mocking Swain", "Swain", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo Menu
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("C_UseQ", "Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("C_UseW", "W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("C_UseE", "E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("C_UseR", "R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("C_MockingSwain", "Use Zhonya while Ult").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("C_MockingSwainSlider", "Zhonya ult at Health (%)").SetValue(new Slider(30, 1, 100)));
            //Harass Menu
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("H_UseQ", "Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("H_UseW", "W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("H_UseE", "E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("H_AutoE", "Auto-E enemies").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("H_ESlider", "Stop Auto E at Mana (%)").SetValue(new Slider(80, 1, 100)));           
            Config.AddToMainMenu();

            //Nerd Shit
            Game.OnGameUpdate += OnGameUpdate;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;
        }

        //Credits to: xQx for this function
        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (!(sender.Name.Contains("swain_demonForm")))
                return;
            RavenForm = true;
        }

        //Credits to: xQx for this function
        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (!(sender.Name.Contains("swain_demonForm")))
                return;
            RavenForm = false;
        }

        //Credits to: xSalice, this is from his Blitzcrank script, I just modified it
        private static bool SafeWCast(Obj_AI_Hero target)
        {
            if (target == null) return false; 

            if (!Q.IsReady())
            {
                if (target.HasBuffOfType(BuffType.Slow) && W.GetPrediction(target).Hitchance >= HitChance.High) return true;
                if (W.GetPrediction(target).Hitchance == HitChance.Immobile) return true;
                if (W.GetPrediction(target).Hitchance == HitChance.VeryHigh) return true;
            }
            return false;
        }


        private static void OnGameUpdate(EventArgs args)
        {
            switch(Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }

            if (Config.Item("H_AutoE").GetValue<bool>())
            {
                AutoE();
            }
        }

        //Thanks to xQx
        private static void AutoE()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var ManaLimit = Player.MaxMana / 100 * Config.Item("H_ESlider").GetValue<Slider>().Value;
            if (Player.Mana <= ManaLimit) return;
            if(E.IsReady())
            {
                E.Cast(target);
            }
        }

        //Thanks to xQx
        private static void MockingSwain()
        {
            var HealthLimit = Player.MaxHealth / 100 * Config.Item("C_MockingSwainSlider").GetValue<Slider>().Value;
            if(RavenForm && Player.Health <= HealthLimit)
            {
                if(Zhonya.IsReady())
                { Zhonya.Cast(); }
            }
        }

        private static void Combo()
        {
            CastSpells(Config.Item("C_UseQ").GetValue<bool>(),
                Config.Item("C_UseW").GetValue<bool>(),
                Config.Item("C_UseE").GetValue<bool>(),
                Config.Item("C_UseR").GetValue<bool>());

            if (Config.Item("C_MockingSwain").GetValue<bool>())
            {
                MockingSwain();
            }
        }

        private static void Harass()
        {
            CastSpells(Config.Item("H_UseQ").GetValue<bool>(),
                Config.Item("H_UseW").GetValue<bool>(),
                Config.Item("H_UseE").GetValue<bool>(), false);
        }

        private static void CastSpells(bool useQ, bool useW, bool useE, bool useR)
        {
            var target = TargetSelector.GetTarget(800, TargetSelector.DamageType.Magical);
            if (target == null) return; 

            //E
            if (E.IsReady() && useE)
            {
                E.Cast(target);
            }

            //Q
            if (Q.IsReady() && useQ) 
            {
                Q.Cast(target);
            }

            //W
            if (target.IsValidTarget(W.Range) && W.IsReady() && SafeWCast(target) && useW)
            {
                var prediction = W.GetPrediction(target);
                W.Cast(prediction.CastPosition);
            }

            //R
            if(R.IsReady() && target.IsValidTarget(R.Range) && !RavenForm && useR)
            {
                R.Cast();   
            }
        }
    }
}
