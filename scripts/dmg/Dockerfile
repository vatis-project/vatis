FROM ubuntu:20.04
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates build-essential cmake zlib1g-dev libbz2-dev libssl-dev python3 python3-pip hfsprogs \
 && rm -rf /var/lib/apt/lists/*
RUN git clone https://github.com/planetbeing/libdmg-hfsplus /src && cd /src \
 && sed -i 's/HMAC_CTX\thmacCTX;/HMAC_CTX *hmacCTX;/' includes/dmg/filevault.h \
 && sed -i -e 's/&(info->hmacCTX)/info->hmacCTX/g' -e 's/HMAC_CTX_cleanup(info->hmacCTX)/HMAC_CTX_free(info->hmacCTX)/' -e 's/HMAC_CTX_init(info->hmacCTX);/info->hmacCTX = HMAC_CTX_new();/' dmg/filevault.c \
 && cmake -B build -DCMAKE_BUILD_TYPE=Release . >/dev/null && cmake --build build -j8 >/dev/null \
 && cp build/dmg/dmg build/hfs/hfsplus /usr/local/bin/
RUN pip3 install ds_store mac_alias
