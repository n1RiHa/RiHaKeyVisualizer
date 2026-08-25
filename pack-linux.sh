#!/bin/bash
set -e

# Читаем версию из файла (с удалением символов \r, если файл из Windows)
version=$(tr -d '\r\n' < VERSION.txt)

# Создаем рабочие директории
mkdir -p tmp/RIHaKeyVisualizer

# Копируем нужные файлы
cp "Info.json" "tmp/RIHaKeyVisualizer/"
cp "riha_on.png" "tmp/RIHaKeyVisualizer/"
cp "riha_left.png" "tmp/RIHaKeyVisualizer/"
cp "riha_right.png" "tmp/RIHaKeyVisualizer/"
cp "riha_off.png" "tmp/RIHaKeyVisualizer/"
cp "bin/Debug/netstandard2.1/RIHaKeyVisualizer.dll" "tmp/RIHaKeyVisualizer/"

# Переходим в рабочую папку
cd tmp/RIHaKeyVisualizer

# Заменяем $VERSION на реальное значение в Info.json прямо на месте
sed -i "s/\$VERSION/$version/g" Info.json

cd ..

# Создаем zip-архив (требуется утилита zip)
zip -r "RIHaKeyVisualizer-${version}-netstandard2.1.zip" RIHaKeyVisualizer

# Перемещаем архив в корень
mv "RIHaKeyVisualizer-${version}-netstandard2.1.zip" ..

cd ..

echo "Сборка успешно завершена!"
read -p "Нажмите Enter для выхода..."