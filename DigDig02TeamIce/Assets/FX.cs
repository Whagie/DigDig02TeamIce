using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FX : MonoBehaviour
{
    private static Dictionary<string, AudioClip> lookup;

    public static AudioClip FX_baneful_ball_charge_up { get; private set; }
    public static AudioClip FX_baneful_ball_shoot { get; private set; }
    public static AudioClip FX_bookshelf_rolling { get; private set; }
    public static AudioClip FX_break_wall { get; private set; }
    public static AudioClip FX_construct_no_energy { get; private set; }
    public static AudioClip FX_crystal_hit { get; private set; }
    public static AudioClip FX_gate_drop { get; private set; }
    public static AudioClip FX_gate_raise { get; private set; }
    public static AudioClip FX_light_puzzle_push_reflector { get; private set; }
    public static AudioClip FX_light_puzzle_receive_light { get; private set; }
    public static AudioClip FX_magic_spear_charge_up { get; private set; }
    public static AudioClip FX_magic_spear_hit { get; private set; }
    public static AudioClip FX_player_attack { get; private set; }
    public static AudioClip FX_player_damage { get; private set; }
    public static AudioClip FX_player_parry { get; private set; }
    public static AudioClip FX_rotate_stone { get; private set; }
    public static AudioClip FX_stab { get; private set; }
    public static AudioClip FX_intro_metal_hits { get; private set; }
    public static AudioClip FX_construct_flap { get; private set; }
    public static AudioClip Music_Combat { get; private set; }
    public static AudioClip Music_NoCombat { get; private set; }
    public static AudioClip Music_MainMenu { get; private set; }

    static FX()
    {
        var prefabs = Resources.LoadAll<AudioClip>("FX");
        lookup = new Dictionary<string, AudioClip>();

        foreach (var prefab in prefabs)
        {
            lookup[prefab.name] = prefab;

            // Auto-map by name
            switch (prefab.name)
            {
                case nameof(FX_baneful_ball_charge_up): FX_baneful_ball_charge_up = prefab; break;
                case nameof(FX_baneful_ball_shoot): FX_baneful_ball_shoot = prefab; break;
                case nameof(FX_bookshelf_rolling): FX_bookshelf_rolling = prefab; break;
                case nameof(FX_break_wall): FX_break_wall = prefab;break;
                case nameof(FX_construct_no_energy): FX_construct_no_energy = prefab; break;
                case nameof(FX_crystal_hit): FX_crystal_hit = prefab; break;
                case nameof(FX_gate_drop): FX_gate_drop = prefab; break;
                case nameof(FX_gate_raise): FX_gate_raise = prefab; break;
                case nameof(FX_light_puzzle_push_reflector): FX_light_puzzle_push_reflector = prefab; break;
                case nameof(FX_light_puzzle_receive_light): FX_light_puzzle_receive_light = prefab; break;
                case nameof(FX_magic_spear_charge_up): FX_magic_spear_charge_up = prefab; break;
                case nameof(FX_magic_spear_hit): FX_magic_spear_hit = prefab; break;
                case nameof(FX_player_attack): FX_player_attack = prefab; break;
                case nameof(FX_player_damage): FX_player_damage = prefab; break;
                case nameof(FX_player_parry): FX_player_parry = prefab; break;
                case nameof(FX_rotate_stone): FX_rotate_stone = prefab; break;
                case nameof(FX_stab): FX_stab = prefab; break;
                case nameof(FX_intro_metal_hits): FX_intro_metal_hits = prefab; break;
                case nameof(FX_construct_flap): FX_construct_flap = prefab; break;
                case nameof(Music_Combat): Music_Combat = prefab; break;
                case nameof(Music_NoCombat): Music_NoCombat = prefab; break;
                case nameof(Music_MainMenu): Music_MainMenu = prefab; break;
            }
        }
    }
}
