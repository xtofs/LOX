
printf 'fun f(a, b) { return a + b; }\nprint f(1, 2);' | dotnet run --project src/demo/demo.csproj --no-restore -- parse /dev/stdin