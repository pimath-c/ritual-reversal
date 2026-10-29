// Fotos de referência (F10 numa partida local): leva o jogador pelas mesmas 17 vistas do tools/navegador.js, com a noite
// limpa e sem o HUD, e grava um PNG de cada uma em <persistentDataPath>/referencia-<data>. Serve para comparar o Unity com o
// navegador lado a lado: o tools/referencia-navegador.js tira as mesmas fotos no navegador. F10 de novo interrompe.
using System;
using System.IO;
using UnityEngine;
using RitualReversal.Simulacao;

namespace RitualReversal
{
    public partial class PartidaLocal
    {
        struct VistaRef { public string nome; public double x, z, yaw, pitch; public VistaRef(string n, double x, double z, double yaw, double pitch) { nome = n; this.x = x; this.z = z; this.yaw = yaw; this.pitch = pitch; } }
        const double PI = Math.PI;
        static readonly VistaRef[] VISTAS_REF = {   // as VISTAS do tools/navegador.js, na mesma ordem
            new VistaRef("acampamento", -122, -22, -PI / 2, .05), new VistaRef("ponte-grande", 0, -2, 0, .25),
            new VistaRef("ponte-caida", -106, -77, -PI / 2, .02), new VistaRef("aqueduto", 106, -77, PI / 2, .02),
            new VistaRef("nave", 0, -38, 0, .12), new VistaRef("cruzeiro", 0, -66, 0, .25), new VistaRef("claustro", -36, -46, -.7, .1),
            new VistaRef("sala-capitulo", -53, -47, PI / 4, -.05), new VistaRef("capela-desabada", 22, -71, -PI / 2, .05),
            new VistaRef("menires", -82, 50, PI, 0), new VistaRef("encapuzados", 82, 52, PI, .05), new VistaRef("portaria", 0, 10, -PI / 2, 0),
            new VistaRef("pedreira", -46, 28, PI, .05), new VistaRef("charco", 46, 30, PI, -.05), new VistaRef("carvoaria", 14, 50, PI, .05),
            new VistaRef("calvario", 0, 76, PI, .1), new VistaRef("ruinas", -120, -76, 0, .05) };
        int refIdx = -1; float refT; bool refGravou; string refPasta, refMatchAntes;
        bool Referenciando { get { return refIdx >= 0; } }

        // chamado no Update, depois da simulação: põe o jogador na vista, espera a imagem assentar e fotografa
        void PassoReferencia(Ator m, bool jogando)
        {
            if (jogando && !online && !chatAberto && EntradaUnity.Apertou(Tecla.F10)) { if (Referenciando) FimReferencia("Fotos interrompidas"); else ComecoReferencia(); }
            if (!Referenciando) return;
            if (!jogando || m == null || g == null) { FimReferencia("Fotos interrompidas"); return; }
            var v = VISTAS_REF[refIdx];
            m.x = v.x; m.z = v.z; m.invuln = 999; m.hp = m.maxHp; lookYaw = v.yaw; lookPitch = v.pitch; m.yaw = v.yaw; m.pitch = v.pitch;
            foreach (var a in g.actors) if (a != m) { a.x = a.team == "H" ? -125 : 125; a.z = a.team == "H" ? -8 : 8; }   // ninguém na frente da câmera
            refT += Time.unscaledDeltaTime;
            if (!refGravou && refT > 2f) { ScreenCapture.CaptureScreenshot(Path.Combine(refPasta, (refIdx + 1).ToString("00") + "-" + v.nome + ".png")); refGravou = true; }
            else if (refGravou && refT > 2.6f) { refIdx++; refT = 0; refGravou = false; if (refIdx >= VISTAS_REF.Length) FimReferencia("Fotos salvas: " + refPasta); }
        }

        void ComecoReferencia()
        {
            refPasta = Path.Combine(Application.persistentDataPath, "referencia-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            try { Directory.CreateDirectory(refPasta); } catch (Exception e) { Anunciar("Não deu para criar a pasta", e.Message); return; }
            // a mesma noite limpa das fotos do navegador: o clima sai do matchId (ClimaDe), e "foto<i>" é o primeiro limpo
            refMatchAntes = g.matchId; int i = 0; while (Visual.Mundo.ClimaDe("foto" + i + ":1") != "limpa") i++; g.matchId = "foto" + i;
            refIdx = 0; refT = 0; refGravou = false;
            Debug.Log("Fotos de referência em " + refPasta);
        }

        void FimReferencia(string msg)
        {
            refIdx = -1; if (g != null && refMatchAntes != null) g.matchId = refMatchAntes; refMatchAntes = null;
            Anunciar(msg); Debug.Log(msg);
        }
    }
}
