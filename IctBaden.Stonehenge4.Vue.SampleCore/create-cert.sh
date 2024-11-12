#!/usr/bin/env bash
openssl req -x509 -newkey rsa:4096 -keyout stonehenge.key -out stonehenge.crt -sha256 -days 3650 -nodes -subj "/C=DE/ST=Baden/L=Durlach/O=ICT Baden GmbH/OU=stonehenge.ict-baden/CN=localhost"
openssl pkcs12 -export -out stonehenge.pfx -inkey stonehenge.key -in stonehenge.crt -password pass:stonehenge

