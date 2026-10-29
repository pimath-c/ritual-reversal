// Cliente online do Unity (Scripts/Rede.cs) contra o servidor Node: sala, lobby, partida, entradas, snapshots,
// previsão do próprio movimento e volta com a senha de retorno. Rodado por tools/rede-cs.js.
using System; using System.Collections.Generic; using System.IO; using System.Threading; using RitualReversal; using RitualReversal.Simulacao;
class T {
  static Dictionary<string,object> Esperar(Conexao c, string tipo, int ms=8000){ var fim=DateTime.Now.AddMilliseconds(ms); string m;
    while(DateTime.Now<fim){ while(c.Receber(out m)){ var d=Json.Ler(m) as Dictionary<string,object>; if((string)d["t"]==tipo) return d; if((string)d["t"]=="error") throw new Exception("erro do servidor: "+d["msg"]); } if(c.fechada) throw new Exception("fechou: "+c.erro); Thread.Sleep(5); }
    throw new Exception("sem "+tipo); }
  static int falhas=0; static void Ok(bool c,string msg){ Console.WriteLine((c?"ok    ":"FALHA ")+msg); if(!c) falhas++; }
  static void Main(string[] a){
    Sim.Iniciar(File.ReadAllText(a[0]));
    var c=new Conexao(); c.Abrir("http://localhost:"+a[1]);
    c.Enviar(new JsonEscrita().Txt("t","hello").Bool("create",true).Txt("code","").Txt("name","Unity \"teste\"").Txt("token","tok123").ToString());
    var w=Esperar(c,"welcome"); string cid=(string)w["cid"], sala=(string)w["code"]; Ok(sala.Length==4,"sala criada "+sala+", cid "+cid);
    var lob=Esperar(c,"lobby"); Ok((string)lob["host"]==cid,"anfitrião é este cliente");
    // segundo cliente entra na mesma sala
    var c2=new Conexao(); c2.Abrir("ws://localhost:"+a[1]); c2.Enviar(new JsonEscrita().Txt("t","hello").Bool("create",false).Txt("code",sala.ToLower()).Txt("name","Dois").Txt("token","tok2").ToString());
    var w2=Esperar(c2,"welcome"); Ok((string)w2["code"]==sala,"segundo cliente entrou pelo código");
    c.Enviar(new JsonEscrita().Txt("t","start").ToString());
    Jogo g=null; var snap=Esperar(c,"snap"); g=Snapshot.Ler((Dictionary<string,object>)snap["s"]);
    Ok(g.altars.Count==6&&g.slots.Count==4,"snapshot: "+g.actors.Count+" atores, "+g.altars.Count+" altares, fase "+g.phase);
    Ator me=null;
    // avança fases: pick (cultistas escolhem; o 2º cliente pode ser cultista), ready
    string papel=Sim.RoleOf(g,g.slots.Find(s=>s.cid==cid).team); Console.WriteLine("papel do cliente 1: "+papel+", rodada "+g.round);
    Action<Conexao,Acao> act=(cc,ac)=>{ var o=new JsonEscrita().Txt("type",ac.type); if(ac.type=="pick") o.Num("i",ac.i); if(ac.cls!=null) o.Txt("cls",ac.cls); if(ac.f!=null) o.Txt("f",ac.f); cc.Enviar(new JsonEscrita().Txt("t","act").Obj("a",o).ToString()); };
    foreach(var cc in new[]{c,c2}){ act(cc,new Acao{type="pickRandom"}); act(cc,new Acao{type="pickConfirm"}); }
    var fim=DateTime.Now.AddSeconds(25); int nsnap=0, nev=0; double x0=0,z0=0; bool andou=false; int seq=0; double erroMax=0; var pend=new List<KeyValuePair<int,Entrada>>(); Ator pred=null;
    DateTime tPlay=DateTime.MinValue; string m2; var tipos=new HashSet<string>();
    while(DateTime.Now<fim){
      while(c2.Receber(out m2)){} // o 2º só escuta
      string m; while(c.Receber(out m)){ var d=Json.Ler(m) as Dictionary<string,object>; if((string)d["t"]!="snap") continue; nsnap++;
        var s=(Dictionary<string,object>)d["s"]; g=Snapshot.Ler(s); foreach(var e in (List<object>)d["ev"]){ nev++; tipos.Add(Snapshot.Evento((Dictionary<string,object>)e).type); }
        if(g.phase=="intro"&&!g.ready.ContainsKey(cid)){ act(c,new Acao{type="ready"}); act(c2,new Acao{type="ready"}); }
        me=g.actors.Find(x=>x.cid==cid); int ack=Convert.ToInt32(d["ack"]);
        if(me!=null&&g.phase=="play"){ if(x0==0&&z0==0){ x0=me.x; z0=me.z; }
          // previsão: estado do servidor + entradas não confirmadas; comparar com a previsão anterior
          var p=Snapshot.Ator(FindD(s,me.id)); if(pred!=null){ p.inp=pred.inp; p.yaw=pred.yaw; p.pitch=pred.pitch; } double ax=pred!=null?pred.x:me.x, az=pred!=null?pred.z:me.z; pred=p;
          pend.RemoveAll(q=>q.Key<=ack); foreach(var q in pend) Sim.MoveHuman(g,pred,q.Value,1/60.0);
          double er=Math.Sqrt((ax-pred.x)*(ax-pred.x)+(az-pred.z)*(az-pred.z)); if(tPlay!=DateTime.MinValue) erroMax=Math.Max(erroMax,er);
          if(Math.Abs(me.x-x0)+Math.Abs(me.z-z0)>3) andou=true; } }
      if(g!=null&&g.phase=="play"&&pred!=null){ // anda para a frente a 60 Hz
        var inp=new Entrada{mz=-1,yaw=pred.yaw,pitch=0}; seq++; Sim.MoveHuman(g,pred,inp,1/60.0); pend.Add(new KeyValuePair<int,Entrada>(seq,inp));
        c.Enviar(new JsonEscrita().Txt("t","in").Num("mx",0).Num("mz",-1).Bool("sp",false).Num("yaw",pred.yaw).Num("pitch",0).Bool("fire",false).Bool("use",false).Bool("use2",false)
          .Obj("c",new JsonEscrita().Num("q",0).Num("f",0).Num("g",0).Num("r",0).Num("ab",0)).Num("dt",1/60.0).Num("viewT",g.t).Num("seq",seq).ToString()); }
      if(g!=null&&g.phase=="play"&&me!=null&&andou&&tPlay==DateTime.MinValue) tPlay=DateTime.Now; if(tPlay!=DateTime.MinValue&&(DateTime.Now-tPlay).TotalSeconds>4) break;
      Thread.Sleep(16); }
    Ok(nsnap>20,"recebeu "+nsnap+" snapshots, "+nev+" eventos ("+string.Join(",",tipos)+")");
    Ok(g!=null&&g.phase=="play","chegou à fase de jogo (fase "+(g!=null?g.phase:"-")+")");
    Ok(me!=null&&me.human,"o próprio ator está no snapshot ("+(me!=null?me.name+", "+me.team+", "+me.cls:"")+")"); Ok(andou,"o servidor moveu o ator pelas entradas do cliente ("+x0.ToString("0.0")+","+z0.ToString("0.0")+" -> "+me.x.ToString("0.0")+","+me.z.ToString("0.0")+")");
    Ok(erroMax<1.0,"correção da previsão pequena (máx "+erroMax.ToString("0.000")+" m)");
    Ok(g.tarefas.ContainsKey("H")&&g.know.Length==6,"tarefas e conhecimento dos altares no snapshot");
    // queda e volta com a mesma senha
    c.Fechar(); Thread.Sleep(500);
    var c3=new Conexao(); c3.Abrir("ws://localhost:"+a[1]); c3.Enviar(new JsonEscrita().Txt("t","hello").Bool("create",false).Txt("code",sala).Txt("name","x").Txt("token","tok123").ToString());
    var w3=Esperar(c3,"welcome"); Ok(w3.ContainsKey("voltou"),"reconectou ao mesmo personagem com a senha de retorno");
    var s3=Snapshot.Ler((Dictionary<string,object>)Esperar(c3,"snap")["s"]); var eu3=s3.actors.Find(x=>x.cid==(string)w3["cid"]); Ok(eu3!=null&&eu3.name==me.name,"de volta como "+(eu3!=null?eu3.name:"?"));
    var oculto=s3.actors.FindAll(x=>x.hidden).Count; Console.WriteLine("inimigos escondidos pelo snapshot agora: "+oculto);
    c2.Fechar(); c3.Fechar();
    Console.WriteLine(falhas==0?"TUDO CERTO":falhas+" FALHA(S)"); Environment.Exit(falhas==0?0:1);
  }
  static Dictionary<string,object> FindD(Dictionary<string,object> s,int id){ foreach(var o in (List<object>)s["actors"]){ var d=(Dictionary<string,object>)o; if(Convert.ToInt32(d["id"])==id) return d; } return null; }
}
