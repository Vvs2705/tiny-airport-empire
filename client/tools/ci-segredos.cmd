@echo off
rem Cadastra os segredos do CI do Unity sem copiar e colar nada. De dois cliques e responda o que for pedido.
rem O conteudo vai direto do arquivo para o GitHub; nada fica gravado no PC nem aparece na tela.
setlocal
set REPO=Vvs2705/tiny-airport-empire
set ULF=C:\ProgramData\Unity\Unity_lic.ulf

if not exist "%ULF%" (
  echo Nao achei %ULF%.
  echo No Unity Hub: engrenagem ^> Licencas ^> Adicionar ^> "Obter licenca Personal gratuita". Depois rode este arquivo de novo.
  pause & exit /b 1
)

echo [1/3] Licenca do Unity...
gh secret set UNITY_LICENSE -R %REPO% < "%ULF%" || goto erro

echo [2/3] E-mail da conta Unity (digite e tecle Enter):
gh secret set UNITY_EMAIL -R %REPO% || goto erro

echo [3/3] Senha da conta Unity (nao aparece ao digitar):
gh secret set UNITY_PASSWORD -R %REPO% || goto erro

echo.
echo Pronto. Segredos cadastrados:
gh secret list -R %REPO%
pause & exit /b 0

:erro
echo Algo falhou acima. Tire um print desta janela e mande para o Claude.
pause & exit /b 1
