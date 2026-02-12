背景：由于火山ecs访问ubuntu apt源不稳定
提前把这两个apt ppa的公钥导出为 .gpg 文件

# git-core PPA 公钥
curl -fsSL 'https://keyserver.ubuntu.com/pks/lookup?op=get&search=0xE1DD270288B4E6030699E45FA1715D88E1DF1F24' \
  | gpg --dearmor -o git-core-ppa.gpg

# deadsnakes PPA 公钥
curl -fsSL 'https://keyserver.ubuntu.com/pks/lookup?op=get&search=0xF23C5A6CF475977595C89F51BA6932366A755776' \
  | gpg --dearmor -o deadsnakes-ppa.gpg