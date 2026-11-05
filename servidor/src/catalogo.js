// O catalogo do servidor: os ids e o tipo de Tabelas.Produtos (client/Assets/_TA/Scripts/Core/Compras.cs). O teste catalogo.test.js le o
// Compras.cs e quebra se os dois divergirem. Nenhuma quantidade aqui: o conteudo sai da tabela do nucleo (GDD §8.2).
export const PACOTE = 'br.com.vstack.littleterminal';

export const PRODUTOS = Object.freeze({
  welcome_bundle_01: 'NaoConsumivel',
  gems_pack_small: 'Consumivel',
  gems_pack_medium: 'Consumivel',
  gems_pack_large: 'Consumivel',
  season_pass_s1: 'NaoConsumivel',
  aircraft_skin_blue_01: 'NaoConsumivel',
  terminal_decor_01: 'NaoConsumivel',
});

export const consumivel = (produto) => PRODUTOS[produto] === 'Consumivel';
