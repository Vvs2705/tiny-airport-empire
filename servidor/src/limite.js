// Limite de taxa por IP, na memoria da instancia (GDD §8.5). O IP so serve para isto: nao vai para log nem para o registro.
// Com o Cloud Run em no maximo 2 instancias, o teto real e' o dobro. Balde: `porMinuto` pedidos, reposto aos poucos.
export function limitador(porMinuto, agora = Date.now) {
  const baldes = new Map();
  return (ip) => {
    const t = agora();
    if (baldes.size > 10000) for (const [k, b] of baldes) if (t - b.t > 60000) baldes.delete(k);
    const b = baldes.get(ip) ?? { n: porMinuto, t };
    b.n = Math.min(porMinuto, b.n + ((t - b.t) * porMinuto) / 60000);
    b.t = t;
    baldes.set(ip, b);
    if (b.n < 1) return false;
    b.n -= 1;
    return true;
  };
}
