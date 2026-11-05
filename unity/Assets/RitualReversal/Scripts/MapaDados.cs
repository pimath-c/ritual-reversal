// Formato de Dados/mapa.json, gerado por tools/exportar-mapa.js a partir de shared/sim.js.
// Metros. No protótipo o chão é o plano x-z com y para cima; no Unity use MapaDados.Pos(x, z) (Z = -z),
// porque o protótipo usa mão direita e o Unity mão esquerda: sem inverter, o mapa sai espelhado.
using System;
using UnityEngine;

namespace RitualReversal
{
    [Serializable] public class PontoXZ { public float x, z; }
    [Serializable] public class Retangulo { public float x1, x2, z1, z2; }
    [Serializable] public class Limites { public PontoXZ meiaLargura, planoOriginal4v4; public Retangulo catedral, claustro; }
    [Serializable] public class SpawnDados { public float x, z, yaw; }
    [Serializable] public class Spawns { public SpawnDados H, C; }
    [Serializable] public class AltarDados { public int i; public string nome; public float x, z; }
    [Serializable] public class MercadorDados { public string id, nome, time; public float x, z; public string[] vende; }
    [Serializable] public class PontosDeTarefa { public PontoXZ[] pista, sentinela, erva, tumulo; }
    [Serializable] public class LuzDados { public float x, z; public string tipo; }
    [Serializable] public class TrilhaDados { public float largura; public bool estreita; public PontoXZ[] pontos; }
    [Serializable] public class ClareiraDados { public float x, z, raio; }
    [Serializable] public class PilarDados { public float x, z, raio, h; }
    [Serializable] public class BancoDados { public float x1, x2, z1, z2, h; }
    [Serializable] public class LugarDados { public string nome; public float x, z; }
    // Peça de colisão: tudo que bloqueia passagem. tall = também bloqueia visão e tiros.
    [Serializable] public class PecaDados { public string tipo; public float x1, x2, z1, z2, h, rot; public bool tall; public int pose; }
    [Serializable] public class ArvoreDados { public float x, z, raio, escala, rot; public string tipo; }

    [Serializable]
    public class MapaDados
    {
        public string sobre;
        public Limites limites;
        public Spawns spawns;
        public AltarDados[] altares;
        public PontoXZ[] reagentes;
        public MercadorDados[] mercadores;
        public PontosDeTarefa pontosDeTarefa;
        public LuzDados[] luzes;
        public TrilhaDados[] trilhas;
        public ClareiraDados[] clareiras;
        public PilarDados[] pilares;
        public BancoDados[] bancos;
        public LugarDados[] lugares;
        public PecaDados[] pecas;
        public ArvoreDados[] arvores;

        // Protótipo (x, y, z) -> Unity (x, y, -z).
        public static Vector3 Pos(float x, float z, float y = 0f) => new Vector3(x, y, -z);
        // Giro em torno de y: o protótipo mede em radianos com mão direita; o Unity em graus com mão esquerda.
        public static float Giro(float rotRad) => -rotRad * Mathf.Rad2Deg;
        // O yaw do protótipo olha para -z quando vale 0; no Unity isso é +Z, e o sentido do giro se inverte.
        public static Quaternion Olhar(float yawRad) => Quaternion.Euler(0f, -yawRad * Mathf.Rad2Deg, 0f);
    }
}
