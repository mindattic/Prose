window.consoleBg = (function () {
    'use strict';

    // ── Window title bar labels ─────────────────────────────────────────────
    var TITLES = [
        'proc/8812',  'proc/9144',  'proc/3301',  'proc/7712',  'proc/4492',
        'proc/1187',  'proc/6603',  'proc/2255',  'proc/5519',  'proc/8001',
        'bci_daemon', 'neural_rx',  'cortex_sync','grip_cls',   'bim_proc',
        'rbs_ctrl',   'hkb_mon',    'port_sel',   'slug_feed',  'buck_feed',
        'kern/sched', 'kern/vm',    'kern/ipc',   'kern/net',   'kern/fs',
        'kern/mm',    'kern/irq',   'kern/sig',   'kern/clock', 'kern/audit',
        'sys/netd',   'sys/logd',   'sys/cryptd', 'sys/authd',  'sys/watchd',
        'sys/arbiter','sys/broker', 'sys/relay',  'sys/mirror', 'sys/janitor',
        'net/tx_mon', 'net/rx_mon', 'net/gate',   'net/proxy',  'net/scanner',
        'net/sniffer','net/balancer','net/tunnel', 'net/beacon', 'net/resolver',
        'db/node_idx','db/edge_map','db/query',   'db/cache',   'db/repl',
        'db/compact', 'db/journal', 'db/txn_log', 'db/vacuum',  'db/snapshot',
        'sec/auditor','sec/policy', 'sec/token',  'sec/vault',  'sec/scanner',
        'sec/ids',    'sec/firewall','sec/probe',  'sec/enclave','sec/signer',
        'glmz/relay', 'glmz/gate',  'glmz/node',  'glmz/border','glmz/comms',
        'crest/rbs',  'crest/bim',  'crest/hkb',  'crest/led',  'crest/cal',
        'corp/enforcer','corp/monitor','corp/audit','corp/sync', 'corp/mirror',
        'dist7/node', 'dist12/hub', 'dist4/fab',  'dist9/relay','dist2/comms',
        'arc/compress','arc/index', 'arc/verify', 'arc/extract','arc/delta',
        'jit/compile', 'jit/cache', 'jit/evict',  'jit/patch',  'jit/trace',
        // 100 new entries
        'bci/rx',      'bci/tx',    'bci/cal',    'bci/cls',    'bci/key',
        'bci/epoch',   'bci/sync',  'bci/auth',   'bci/mon',    'bci/log',
        'rbs/alpha',   'rbs/beta',  'rbs/motor',  'rbs/detent', 'rbs/led',
        'rbs/pin',     'rbs/feed',  'rbs/jam',    'rbs/cal',    'rbs/therm',
        'hkb/piston',  'hkb/fluid', 'hkb/spring', 'hkb/cycle',  'hkb/therm',
        'hkb/wear',    'hkb/gas',   'hkb/recoil', 'hkb/buf',    'hkb/mon',
        'bim/cls',     'bim/infer', 'bim/train',  'bim/feat',   'bim/lock',
        'bim/verify',  'bim/cal',   'bim/model',  'bim/log',    'bim/adc',
        'node/glmz7',  'node/glmz4','node/glmz9', 'node/glmz2', 'node/glmz12',
        'relay/d4',    'relay/d7',  'relay/d9',   'relay/d12',  'relay/d2',
        'enclave/sgx', 'enclave/key','enclave/attest','enclave/seal','enclave/log',
        'freelancer/auth','freelancer/id','freelancer/track','freelancer/zone','freelancer/log',
        'corp/ledger', 'corp/quota','corp/enforce','corp/cred',  'corp/revoke',
        'crest/fw',    'crest/patch','crest/diag', 'crest/ring', 'crest/batt',
        'dist/arbiter','dist/broker','dist/fabric', 'dist/mesh',  'dist/topo',
        'net/mesh',    'net/dark',  'net/border',  'net/encap',  'net/decrypt',
        'sec/hsm',     'sec/kdf',   'sec/acl',     'sec/mfa',    'sec/crl',
        'db/graph',    'db/edge_idx','db/node_map','db/bloom',   'db/mvcc',
        'kern/ebpf',   'kern/kprobe','kern/perf',  'kern/cgroup','kern/ns',
    ];

    // ── Console output lines ────────────────────────────────────────────────
    var LINES = [
        // original 28
        '0x4f3a :: dispatch_sync[0x1b] → q',
        'pid:8812 ipc.recv buf=0xffc2 ok',
        'kern: mmap 0x7ffe3200 sz=4096 rw',
        'net.tx 172.16.44.12:5555 1400B',
        'sys.fork → child:9144 inherit_fd',
        'ring3 trap #14 cr2=0x00000008',
        'sched: ctx-swap 8812→9144 Δ=2μs',
        'fs.read /proc/9144/maps 4096B',
        'db.exec idx_scan nodes n=847',
        'alloc: 0x5582c4a0 64B heap ok',
        'lib: dlopen libneural.so.3 ok',
        'audit: cap_check euid=1000 NET',
        'rx:127.0.0.1:44312 frame 0xd4',
        'jit: compile fn@0x5582c4b8 ok',
        'sig: SIGUSR2 pid:8812 → queued',
        'bim: grip[Δ=+0.027] → slug',
        'rbs: rotate 38ms port_beta lock',
        'bci: cortex_rx lat=4ms handshake',
        'net.syn ACK 0x9f3c seq++',
        'vm: page_fault 0x800 recover',
        'cache: evict lru sz=256K ok',
        'ipc.gate:0x1b [enc payload fwd]',
        'sec: token_verify uid=0 ok',
        'crypt: aes256 blk=0x44 done',
        'tsk: worker:12 idle→run lat=1ms',
        'buf: flush 0x3c00 sz=8192B ok',
        'ev: POLLIN fd=7 consumed',
        'arc: compress ratio=2.4 ok',
        // 100 new entries
        'bci: motor_cortex signal lat=3ms',
        'bci: grip_tension[L]=0.41 stable',
        'bci: intent_cls → precision ok',
        'bci: calibrate epoch=7 Δ=0.003',
        'bci: pair sig 0xBF44 accepted',
        'bci: extensor_detect 12ms lock',
        'bci: sweep_cls → buck confirmed',
        'bci: neural_key 0xA3F7 valid',
        'bci: contact_ring[3] Δ=+0.014',
        'bci: 4hr_cal acc=97.3% commit',
        'rbs: disc idle → alpha port',
        'rbs: beta_lock acquired 38ms',
        'rbs: detent spring load=2.1N',
        'rbs: motor_cmd enc[0x22] ok',
        'rbs: yellow_led transit 38ms',
        'rbs: red_led port_alpha active',
        'rbs: manual_override lever→buck',
        'hkb: piston stroke 8mm recoil',
        'hkb: impulse 8ms→120ms spread',
        'hkb: perceived_kick -40% ok',
        'comp: port1 gas_vent upward ok',
        'comp: muzzle_flip correction ok',
        'port_alpha: round_count=2 ok',
        'port_beta: slug_seated fwd ok',
        'port_beta: brass_cap ejector ok',
        'led: pin_tactile protrude slug',
        'net.tx 10.44.7.3:8443 tls1.3',
        'net.rx 192.168.44.12 ack=0x3c',
        'net: retransmit seq=0x9f3d #2',
        'net: route 10.0.0.0/8 via gw4',
        'net: dns glmz.relay.7 → ok',
        'net: keepalive dist12 ok 44ms',
        'net: tls_handshake → resumed',
        'net: cipher AES-256-GCM ok',
        'net: drop 172.31.0.0 blacklist',
        'net: nat 10.44.7.3→203.0.113.9',
        'glmz: border_node[7] ping 12ms',
        'glmz: district12 relay online',
        'glmz: gate_check corp_key ok',
        'glmz: freelancer_id 0x3301 seen',
        'glmz: comm_node district4 ok',
        'glmz: enforcement_ping no reply',
        'glmz: dark_node 10.x seen 1x',
        'glmz: relay_hop 7→12 lat=18ms',
        'corp: sync_pulse ack 0x44 ok',
        'corp: audit_log appended row 8k',
        'corp: policy_check PASS uid=0',
        'corp: rekey_interval 3600s ok',
        'corp: enclave_attest sig valid',
        'corp: mirror_sync lag=44ms ok',
        'db: edge_insert n=3 commit ok',
        'db: node_update id=0x3301 ok',
        'db: idx_rebuild edges done',
        'db: page_split btree 0x4f ok',
        'db: wal_checkpoint frames=2048',
        'db: vacuum reclaim 12 pages',
        'db: query_plan cost=84 hash-join',
        'db: lock shared tbl:nodes ok',
        'db: txn_begin iso=read-committed',
        'db: txn_commit lsn=0x3af7 ok',
        'mem: brk 0x5582c800 +4096 ok',
        'mem: munmap 0x7f3a0000 ok',
        'mem: huge_page 2M mapped ok',
        'mem: oom_score pid:8812 adj=-8',
        'mem: slab reclaim 64 objects',
        'mem: rss_cur=148M rss_peak=201M',
        'mem: swap_in 4 pages 2ms',
        'mem: numa_balance migrate ok',
        'fs: open /var/log/bci.log rw',
        'fs: fsync /var/db/nodes.db ok',
        'fs: inode 8191 ref++ ok',
        'fs: dentry_cache hit 0x3301',
        'fs: dirty_pages flush 32 ok',
        'fs: ext4 blk_alloc 0x3af grp2',
        'fs: journal commit seq=0x1b ok',
        'crypt: chacha20 stream init ok',
        'crypt: hmac-sha256 verify ok',
        'crypt: ecdh p256 derive ok',
        'crypt: rng reseed RDRAND ok',
        'crypt: x25519 keyex done ok',
        'crypt: kdf pbkdf2 iter=100k ok',
        'sec: iptables ACCEPT dst:8443',
        'sec: uid=1000 cap drop NET_RAW',
        'sec: seccomp allow:read,write',
        'sec: ptrace denied pid:9144',
        'sec: selinux enforce domain ok',
        'sec: cert_verify depth=2 ok',
        'sec: crl_check revoked=false',
        'sec: vault_unseal shard 2/3 ok',
        'sched: load avg 0.82 0.74 0.61',
        'sched: preempt lat p99=88μs',
        'sched: cgroup cpu.shares=512',
        'sched: rr timeslice 5ms reset',
        'irq: softirq NET_RX 14 fired',
        'irq: tasklet bci_rx queued',
        'irq: affinity cpu2 set ok',
        'pci: dev 0:1b.0 dma_map ok',
        'usb: dev 3-1.2 xfer 512B ok',
        'arc: lz4 ratio=3.1 blk=64K',
        'arc: delta_encode base=0x3a ok',
        'arc: integrity sha1 match ok',
        'jit: trace loop@0x5582 hot',
        'jit: inline depth=3 ok',
        'jit: deopt guard miss patch',
        'ipc: shm_attach key=0x3301 ok',
        'ipc: mq_send qid=4 sz=64 ok',
        'ipc: sem_wait set=0 val→0',
        // 100 new entries
        'bci: ring0 baseline 2044 stable',
        'bci: ring1 baseline 2049 stable',
        'bci: ring2 noise 0.9μV ok',
        'bci: ring3 noise 1.1μV ok',
        'bci: sweep_score=0.88 accepted',
        'bci: precision_score=0.91 accepted',
        'bci: intent_cls v4 loaded ok',
        'bci: model_hash sha256 match ok',
        'bci: session_id 0xA3F7 opened',
        'bci: session_id 0xA3F7 closed',
        'bci: epoch=9441 complete ok',
        'bci: epoch=9442 complete ok',
        'bci: delta_sync 4.4ms ok',
        'bci: neural_rx overflow drop 1',
        'bci: pair_key 0xB2C9 accepted',
        'bci: foreign_sig 0x1234 blocked',
        'bci: batt 44% est 3.1h remain',
        'bci: power_mode eco active',
        'rbs: disc_temp 41C nominal',
        'rbs: disc_temp 52C elevated!',
        'rbs: port_alpha round_count=8',
        'rbs: port_beta round_count=4',
        'rbs: led_ctrl rgb 0x00ff88 ok',
        'rbs: detent_spring 2.2N ok',
        'rbs: rotation_cmd 180° queued',
        'rbs: jam_sensor clear ok',
        'rbs: feed_sensor ch=a full',
        'rbs: feed_sensor ch=b empty!',
        'hkb: psi_target 220 achieved',
        'hkb: psi_target 200 achieved',
        'hkb: impulse_log 3.8N 8ms ok',
        'hkb: impulse_log 5.1N 9ms ok',
        'hkb: wear_flag service imminent',
        'hkb: fluid_add 40mL logged ok',
        'hkb: spring_replace due flag',
        'hkb: cycle_log entry #9812 ok',
        'bim: feat_vec dim=8 norm ok',
        'bim: classifier_score 0.96 ok',
        'bim: classifier_score 0.62 low',
        'bim: lock_state engaged ok',
        'bim: lock_state released ok',
        'bim: retrain triggered ok',
        'bim: retrain complete acc=98.7%',
        'net.tx enclave:9443 640B tls1.3',
        'net.rx enclave:9443 480B ok',
        'net.tx dist2.comms:7070 288B',
        'net.rx dist2.comms:7070 192B',
        'net.tx dark-relay:10999 96B enc',
        'net.mss 1460 ok',
        'net.frag reassemble id=0x7a ok',
        'net.ecn CE mark rate=0.03',
        'net.pacing rate 8Mbps set',
        'net.arp table 44 entries ok',
        'net.ndp cache flush ok',
        'net.flow label 0x3a441 ok',
        'net.mpls label push 0x3af',
        'net.vpn tunnel corp-d12 up ok',
        'net.vpn tunnel corp-d12 rekey',
        'glmz: sector_9D permit ok',
        'glmz: sector_7A permit revoked!',
        'glmz: checkpoint-14 pass ok',
        'glmz: checkpoint-22 fail deny!',
        'glmz: rf_tag 0x4412 scanned ok',
        'glmz: rf_tag 0x0000 unknown!',
        'glmz: bio_iris match ok',
        'glmz: bio_iris fail reject!',
        'glmz: audio_anomaly d4 flagged',
        'glmz: cam_d9 feed restored ok',
        'corp: dlp_check outbound ok',
        'corp: dlp_check outbound block!',
        'corp: edr_scan pid=9144 clean',
        'corp: edr_scan pid=3301 threat!',
        'corp: siem_event L3 correlate',
        'corp: siem_rule 441 triggered!',
        'corp: patch_mgr 3 updates ok',
        'corp: vuln_scan CVE-2025-4412!',
        'corp: forensic chain ok',
        'corp: incident-9812 opened',
        'sec: acl_rule 44 applied ok',
        'sec: acl_rule 88 deny uid=3301',
        'sec: mfa_totp verify ok',
        'sec: mfa_totp fail 3x!',
        'sec: crl_fetch update ok',
        'sec: crl_check 0x4492 clean',
        'sec: hsm_sign rsa4096 ok',
        'sec: hsm_derive p256 ok',
        'sec: pin_fail count=2 warn',
        'sec: pin_ok uid=0x7712 ok',
        'db: graph_traverse n=88 ok',
        'db: graph_path dist=4 found',
        'db: graph_cycle n=3 break ok',
        'db: mvcc snap lsn=0x3af7 ok',
        'db: bloom false_pos rate=0.01',
        'db: btree_split page=0x4f ok',
        'db: btree_merge page=0x51 ok',
        'db: row_count nodes=9,441',
        'db: row_count edges=44,120',
        'db: sst_compact ratio=3.2 ok',
        'kern: ebpf_map node_hits 441',
        'kern: kprobe bci_rx fired ok',
        'kern: perf_event cpu0 sample',
        'kern: cgroup_mem limit 512M ok',
        'kern: ns_pid isolate ok',
    ];

    // ── Exit results ────────────────────────────────────────────────────────
    var OK_RESULTS = [
        'OK   exit:0  Φ',
        'OK   sync verified',
        'OK   handshake accepted',
        'OK   gate:open',
        'OK   0 faults',
        'OK   dispatch complete',
        'OK   checksum pass',
        'OK   commit 0x3af7',
        'OK   calibration saved',
        'OK   relay ack',
        'OK   token valid',
        'OK   neural lock',
        'OK   4 pages reclaimed',
        'OK   cert verified',
        'OK   latency within SLA',
        // 100 new entries
        'OK   bci pair accepted  Φ',
        'OK   rbs port alpha set',
        'OK   rbs port beta set',
        'OK   hkb impulse ok',
        'OK   bim classify ok',
        'OK   grip intent sweep',
        'OK   grip intent precision',
        'OK   neural key valid',
        'OK   cortex sync 4ms',
        'OK   epoch committed',
        'OK   vault unsealed  Φ',
        'OK   enclave attested',
        'OK   tls 1.3 resumed',
        'OK   relay ack dist12',
        'OK   relay ack dist4',
        'OK   relay ack dist7',
        'OK   relay ack dist9',
        'OK   db txn commit ok',
        'OK   db snapshot ok',
        'OK   wal checkpoint ok',
        'OK   sec scan clean',
        'OK   cert chain valid',
        'OK   token refreshed  Φ',
        'OK   mfa verified ok',
        'OK   acl check pass',
        'OK   hsm sign ok',
        'OK   kdf derive ok',
        'OK   hmac verified ok',
        'OK   aes-gcm tag ok',
        'OK   chacha20 seal ok',
        'OK   ed25519 verify ok',
        'OK   ecdsa p256 ok',
        'OK   rsa4096 sign ok',
        'OK   x25519 exchange ok',
        'OK   rng reseeded ok',
        'OK   slab alloc ok',
        'OK   mmap anon ok',
        'OK   huge page ok',
        'OK   gc collect ok',
        'OK   heap compact ok',
        'OK   jit compile ok',
        'OK   jit install ok',
        'OK   trace patched ok',
        'OK   ipc shm ok',
        'OK   mutex acquired ok',
        'OK   condvar signal ok',
        'OK   barrier pass ok',
        'OK   futex wake ok',
        'OK   pipe flush ok',
        'OK   epoll event ok',
        'OK   socket bound ok',
        'OK   accept conn ok',
        'OK   net gate pass',
        'OK   dns resolved ok',
        'OK   tls cert ok',
        'OK   http2 ok 200',
        'OK   grpc call ok',
        'OK   websocket ping ok',
        'OK   quic stream ok',
        'OK   bgp route ok',
        'OK   fs fsync ok',
        'OK   fs journal ok',
        'OK   fs snapshot ok',
        'OK   fs quota ok',
        'OK   arc lz4 ok',
        'OK   arc zstd ok',
        'OK   arc brotli ok',
        'OK   arc manifest ok',
        'OK   arc checksum ok',
        'OK   arc delta ok',
        'OK   arc dedup ok',
        'OK   crypt sealed ok',
        'OK   crypt kdf ok',
        'OK   crypt nonce ok',
        'OK   crypt iv ok',
        'OK   crypt tag ok',
        'OK   crypt salt ok',
        'OK   crypt block ok',
        'OK   sec session ok',
        'OK   sec role ok',
        'OK   sec policy ok',
        'OK   sec ids clear',
        'OK   sec fw allow',
        'OK   sec key rotate  Φ',
        'OK   glmz gate open',
        'OK   glmz border ok',
        'OK   glmz relay ok',
        'OK   glmz node up',
        'OK   glmz permit ok',
        'OK   glmz rf tag ok',
        'OK   glmz bio ok',
        'OK   corp sync ok',
        'OK   corp enclave ok',
        'OK   corp policy ok',
        'OK   corp audit ok',
        'OK   corp license ok',
        'OK   corp cred ok',
        'OK   corp dlp ok',
        'OK   corp patch ok',
        'OK   corp heartbeat ok',
        'OK   exit:0 clean  Φ',
    ];

    var ERR_RESULTS = [
        'ERR  timeout gate:0x1b',
        'ERR  seg fault cr2=null',
        'ERR  auth rejected',
        'ERR  ipc EOF unexpected',
        'ERR  ENOMEM alloc failed',
        'ERR  checksum mismatch',
        'ERR  sig lost pid:8812',
        'ERR  neural desync',
        'ERR  rbs jam detected',
        'ERR  relay no ack 3x',
        'ERR  cert revoked',
        'ERR  heap corrupted',
        'ERR  lock timeout 500ms',
        'ERR  bci pair lost',
        'ERR  port beta empty',
        // 100 new entries
        'ERR  bci ring2 contact lost',
        'ERR  bci cortex desync',
        'ERR  bci neural_rx overflow',
        'ERR  bci model_hash mismatch',
        'ERR  bci epoch overflow',
        'ERR  bci sweep lock fail',
        'ERR  bci precision lock fail',
        'ERR  bci foreign key injected',
        'ERR  bci session abort',
        'ERR  bci batt critical 4%',
        'ERR  rbs disc stuck',
        'ERR  rbs motor fault',
        'ERR  rbs detent fail',
        'ERR  rbs jam uncleared',
        'ERR  rbs misfire',
        'ERR  rbs thermal shutdown',
        'ERR  rbs feed empty ch=a',
        'ERR  rbs feed empty ch=b',
        'ERR  rbs bolt jam',
        'ERR  rbs trigger fail',
        'ERR  hkb piston jam',
        'ERR  hkb fluid empty',
        'ERR  hkb spring break',
        'ERR  hkb thermal cutoff',
        'ERR  hkb cycle sensor fail',
        'ERR  bim classify fail',
        'ERR  bim model corrupt',
        'ERR  bim calibrate abort',
        'ERR  bim lock timeout',
        'ERR  bim adc fault ring=3',
        'ERR  vault seal broken',
        'ERR  vault shard 1/3 fail',
        'ERR  vault key revoked',
        'ERR  enclave attest fail',
        'ERR  enclave pcr mismatch',
        'ERR  tls handshake fail',
        'ERR  cert expired',
        'ERR  cert revoked chain',
        'ERR  cert chain broken',
        'ERR  token expired',
        'ERR  token revoked',
        'ERR  mfa fail 3x',
        'ERR  acl deny uid=3301',
        'ERR  acl deny uid=4492',
        'ERR  hsm connect fail',
        'ERR  kdf fail entropy',
        'ERR  hmac mismatch',
        'ERR  gcm tag invalid',
        'ERR  rng entropy low',
        'ERR  key import fail',
        'ERR  db corrupt page=0x4f',
        'ERR  db wal truncated',
        'ERR  db deadlock abort',
        'ERR  db index corrupt',
        'ERR  db txn conflict',
        'ERR  db snapshot fail',
        'ERR  db repl gap',
        'ERR  fs journal abort',
        'ERR  fs disk full',
        'ERR  fs inode exhaust',
        'ERR  fs write EIO',
        'ERR  arc corrupt chunk',
        'ERR  arc manifest fail',
        'ERR  arc delta miss',
        'ERR  arc checksum fail',
        'ERR  jit compile abort',
        'ERR  jit cache full',
        'ERR  jit trace abort',
        'ERR  jit regalloc fail',
        'ERR  ipc deadlock pid=8812',
        'ERR  ipc mutex timeout',
        'ERR  ipc shm attach fail',
        'ERR  ipc queue full',
        'ERR  net tunnel down',
        'ERR  net relay 3x timeout',
        'ERR  net gate deny 403',
        'ERR  net dns NXDOMAIN',
        'ERR  net tcp reset peer',
        'ERR  net packet loss 20%',
        'ERR  net bandwidth starved',
        'ERR  glmz gate blacklist',
        'ERR  glmz border deny',
        'ERR  glmz permit expired',
        'ERR  glmz node offline',
        'ERR  glmz rf unknown tag',
        'ERR  glmz bio reject',
        'ERR  corp sync lost',
        'ERR  corp policy deny',
        'ERR  corp enclave fail',
        'ERR  corp license revoked',
        'ERR  corp cred expired',
        'ERR  corp audit gap',
        'ERR  corp dlp blocked',
        'ERR  corp edr threat',
        'ERR  sec ids alert L4',
        'ERR  sec firewall deny',
        'ERR  sec rootkit sig',
        'ERR  sec exfil detected',
        'ERR  sec escalation block',
        'ERR  sec intrusion block',
        'ERR  oom kill pid=3301',
    ];

    // ── Fatal error dialog ──────────────────────────────────────────────────
    var FATAL_TITLES = [
        // original 7
        'FATAL — kernel panic',
        'FATAL — process crash',
        'CRITICAL — unhandled exception',
        'PANIC — memory violation',
        'FATAL — core dump',
        'CRITICAL — stack overflow',
        'FATAL — assertion failed',
        // 100 new entries
        'FATAL — bci_daemon abort',
        'FATAL — neural sync lost',
        'FATAL — rbs motor fault',
        'FATAL — grip classifier panic',
        'FATAL — bim chip unresponsive',
        'FATAL — port alpha jam',
        'FATAL — port beta misfire',
        'FATAL — hydraulic buffer fail',
        'FATAL — neural key revoked',
        'FATAL — cortex signal lost',
        'CRITICAL — heap corruption',
        'CRITICAL — use-after-free',
        'CRITICAL — buffer overflow',
        'CRITICAL — integer overflow',
        'CRITICAL — null dereference',
        'CRITICAL — race condition',
        'CRITICAL — deadlock detected',
        'CRITICAL — ipc gate closed',
        'CRITICAL — auth token expired',
        'CRITICAL — tls cert revoked',
        'CRITICAL — db corruption',
        'CRITICAL — wal log truncated',
        'CRITICAL — index corruption',
        'CRITICAL — disk full abort',
        'CRITICAL — fd limit exceeded',
        'CRITICAL — net partition',
        'CRITICAL — relay timeout 3x',
        'CRITICAL — glmz node offline',
        'CRITICAL — corp sync lost',
        'CRITICAL — enclave attest fail',
        'PANIC — double free',
        'PANIC — stack smash',
        'PANIC — wild pointer write',
        'PANIC — irq storm',
        'PANIC — vm corruption',
        'PANIC — slab underflow',
        'PANIC — dma overrun',
        'PANIC — watchdog timeout',
        'PANIC — nmi received',
        'PANIC — cpu lockup',
        'ABORT — assertion gate.cc',
        'ABORT — invariant violated',
        'ABORT — contract breach',
        'ABORT — precondition fail',
        'ABORT — postcondition fail',
        'ABORT — type confusion',
        'ABORT — format string fault',
        'ABORT — canary smashed',
        'ABORT — signal 11 SIGSEGV',
        'ABORT — signal 6 SIGABRT',
        'ABORT — signal 4 SIGILL',
        'ABORT — signal 8 SIGFPE',
        'ERROR — out of memory',
        'ERROR — device io fault',
        'ERROR — disk read error',
        'ERROR — journal replay fail',
        'ERROR — snapshot corrupt',
        'ERROR — replication lag',
        'ERROR — network unreachable',
        'ERROR — dns resolution fail',
        'ERROR — tls handshake fail',
        'ERROR — certificate expired',
        'KERNEL — divide by zero',
        'KERNEL — bad page table',
        'KERNEL — invalid opcode',
        'KERNEL — general protection',
        'KERNEL — page fault #14',
        'KERNEL — machine check',
        'KERNEL — oops in slab',
        'KERNEL — kasan report',
        'KERNEL — kcsan data race',
        'KERNEL — lockdep violation',
        'CREST — rbs disc shattered',
        'CREST — bim chip fused',
        'CREST — neural key tamper',
        'CREST — hkb piston seized',
        'CREST — battery critical 0%',
        'GLMZ — gate blacklist',
        'GLMZ — border lockdown',
        'GLMZ — district feed cut',
        'GLMZ — enforcement purge',
        'GLMZ — dark node exposure',
        'CORP — policy violation',
        'CORP — audit trail break',
        'CORP — enclave compromise',
        'CORP — zero-day triggered',
        'CORP — exfil detected',
        'BCI — motor signal flood',
        'BCI — calibration corrupt',
        'BCI — foreign key injection',
        'BCI — denial-of-sensation',
        'BCI — interface lockout',
        'SEC — intrusion confirmed',
        'SEC — privilege escalation',
        'SEC — lateral move detected',
        'SEC — exfil in progress',
        'SEC — rootkit signature',
    ];

    var FATAL_MSGS = [
        // original 7
        'NullPointerException at 0x5582c4b8\nThread 8812 terminated — backtrace dumped\nCore written to /tmp/core.8812',
        'Segmentation fault (core dumped)\nAddress 0x00000000 not mapped\nSignal 11 (SIGSEGV) at pid:9144',
        'EXCEPTION_ACCESS_VIOLATION 0xc0000005\nRead from 0x00000000000000a8\nModule: libneural.so.3 +0x4f3a',
        'Stack smashing detected\nAbort trap: 6\nProgram: bci_daemon pid:8812',
        'Assertion failed: ptr != nullptr\nFile: gate.cc line 1403\nAborting pid:8812',
        'Out of memory: kill process 8812\noom_score_adj=0 vm_rss=1.9G\nKernel panic — not syncing',
        'Double free detected in tcache 2\nCorrupted pointer: 0x5582c4a0\nAbort trap — heap integrity check',
        // 100 new entries
        'bci_daemon: neural handshake timeout 5000ms\nGrip classifier lost contact pid:8812\nBIM chip not responding — abort',
        'BIM accuracy drop: 61.2% (threshold 80%)\nCalibration epoch corrupted block 7\nReverting to factory defaults — manual override required',
        'RBS disc rotation stall at 22ms\nExpected 38ms full cycle — motor fault\nPort selection indeterminate — weapon unsafe',
        'Cortex signal flood detected\n12,400 events/ms — saturation threshold 800\nNeural interface shutting down pid:8812',
        'Neural key 0xA3F7 rejected by corp gate\nBIM chip reflashed — foreign employer signature\noperator locked out of own weapon',
        'HKB piston seized at stroke 3.1mm\nExpected 8mm — buffer failure\nRecoil unmitigated — operator injury risk',
        'Port beta empty — slug misfire\nExtractor found no casing in chamber\nRBS returned to alpha — 0 rounds remaining',
        'use-after-free in libneural.so.3 @ 0x7f3a\nHeap metadata overwritten addr=0x5582c4a0\nAddressSanitizer: SEGV on unknown address',
        'Stack buffer overflow in grip_classify()\nReturn address overwritten with 0x4141414141\nPossible exploitation attempt — abort',
        'Kernel OOPS: unable to handle paging request\nvirtual address 0xfffffffffffffff8\nRIP: bci_core+0x3a4/0x5c0',
        'KASAN: use-after-free in bim_calibrate()\nRead of size 8 at addr ffff888103a4\nBuggy address in object freed at bim_init+0x12',
        'Lockdep violation: possible deadlock\nhold: &bci_lock → &rbs_lock\nwait: &rbs_lock → &bci_lock  (cycle)',
        'Machine check: corrected error\nBank 4 MSR 0x411 = 0xbe00000000800400\nMemory controller: DRAM ECC correctable',
        'irqbalance: cpu2 lockup — self-test fail\nNMI watchdog: BUG soft lockup CPU#2 stuck 22s\nKernel panic: not syncing',
        'TCP connection reset by enforcement node\nconn 10.44.7.3:8443 → 172.16.0.1:443\nRST injected mid-handshake — active intercept?',
        'TLS certificate verification failed\ncert: glmz.relay.dist7 CN mismatch\nchain: expired intermediate 2024-11-30',
        'GLMZ gate 0x1b: blacklist match uid=3301\nDistrict 4 access revoked enforcement order\nAll outbound routes blocked — isolated',
        'District 12 relay offline — no heartbeat 120s\nFallback route dist7→dist9 congested 88%\nPacket loss 34% — connection degraded',
        'Corporate audit trail integrity check failed\nBlock 8192 hash mismatch expected=0x3af7\nTampering suspected — incident flagged',
        'Corp enclave attestation signature invalid\nExpected PCR[7]=0x2a3f got 0x0000\nPossible hypervisor injection — lockdown',
        'DB node corruption detected page 0x3301\nFSM state INVALID for block group 7\nFilesystem unmounted — journal replay required',
        'WAL log truncated unexpectedly at lsn 0x3af7\nExpected 4096 frames found 2048\nDatabase unrecoverable without backup',
        'Heap corruption detected in db/edge_insert\nChunk size 0x80 but next_size=0x21 (mismatch)\nMalloc abort — heap metadata invalid',
        'integer overflow in pkt_len calculation\nlen=0xffff0001 overflow wraps to 1\nBuffer underrun — memory corruption imminent',
        'format string vulnerability triggered\nprintf(user_buf) — controlled format string\nprocess 9144 terminated — possible exploit',
        'Signal 11 (SIGSEGV) at address 0x10\nLikely null pointer dereference + offset\npid 8812 bci_daemon — core dumped',
        'Signal 4 (SIGILL) — illegal instruction\nInvalid opcode at RIP=0x5582c4f0\nPossible code corruption or JIT error',
        'Signal 8 (SIGFPE) — arithmetic exception\nDivide by zero at net_rate_calc+0x4a\npid:8812 — process terminated',
        'OOM killer invoked\nKilling process 8812 (bci_daemon) score=892\nMem: 2097152kB total, 12kB free',
        'Disk full — /var/log 100% capacity\nWrite failed: No space left on device\nbci calibration log truncated — data loss',
        'fd limit exceeded: ulimit -n=1024\nprocess 8812 cannot open new file descriptors\nNeural calibration file handle leak suspected',
        'inode exhaustion on /var/bci\n0 inodes remaining on filesystem\nCannot create new calibration epoch files',
        'Syscall audit: unexpected execve() by pid 9144\nParent: bci_daemon — no exec expected\nPossible code injection — process sandboxed',
        'Seccomp violation: sys_ptrace by pid:9144\nKilled by SIGSYS — sandbox policy\nIntrusion attempt logged uid=1000',
        'SELinux denial: bci_daemon → corp_vault_t\npermission read on file /var/vault/key.pem\nAVC denied — policy enforcement active',
        'capabilities violation: CAP_NET_RAW denied\npid:9144 attempted raw socket creation\nProcess killed SIGKILL — policy enforced',
        'DMA overrun detected pci 0:1b.0\nDMA write outside mapped region 0x3af7000\nIOMMU fault — device reset required',
        'USB device 3-1.2 disconnected mid-transfer\nExpected 4096B transferred 1024B\nbci interface offline — reconnect required',
        'PCIe link down: device 0:1e.0\nlink_speed 8GT/s → link_down\nbci adapter offline — kernel driver error',
        'nvme0 I/O error: LBA 0x3af7 read fail\nError code: 0x4 (aborted command)\nFilesystem marked read-only — I/O error',
        'RAID array degraded: device /dev/sdb failed\n1 of 2 mirrors offline — no redundancy\nImminent data loss — replace drive',
        'Snapshot 0x3301 integrity check failed\nChecksum mismatch block 4096 of 8192\nRestore aborted — source corrupted',
        'Replication lag exceeds threshold: 44s\nPrimary LSN 0x3af7 replica 0x3a00\nAutomatic failover blocked — manual intervention',
        'Index 0x3301 B-tree root corrupted\nPage header magic 0xDEAD expected 0x3141\nDatabase offline — rebuild from backup',
        'Journal replay failed at lsn 0x1b\nTransaction incomplete — data loss\nManual recovery required: fsck.ext4',
        'SMART threshold exceeded: reallocated sectors=512\nPredicted failure imminent drive /dev/sda\nBackup immediately — hardware failure',
        'Memory ECC uncorrectable error\nDIMM slot A1 addr=0x7ffe3200\nSystem halted — hardware fault',
        'CPU thermal throttle: package temp 94°C\nPerformance reduced — cooling system fault\nProcess bci_daemon missed 22 deadlines',
        'Watchdog timeout: bci_daemon 30s no heartbeat\nAutomatic restart failed — respawn limit 5\nManual intervention required',
        'Zombie process storm: 4092 zombies\nParent pid:8812 not reaping children\nPID table exhaustion imminent',
        'Thread 8812 blocked 5min on mutex\nPossible deadlock: holder=thread 9144\nBacktrace: bim_calibrate → rbs_select → bci_sync',
        'CORP zero-day: privilege escalation attempt\nCVE-2025-8812 libneural.so.3 heap overflow\nPatch not applied — system vulnerable',
        'Rootkit signature detected in /proc\nhidden PID 31337 not in task_struct list\nKernel integrity compromised — incident',
        'Lateral movement detected\nuid=1000 → uid=0 via CVE-2025-8812\nPivot to corp_vault — exfil in progress',
        'Intrusion confirmed: corp_enclave breached\nForeign key material extracted — 4096B\nZero-day exploit — containment failed',
        'Network partition: district 7 → district 12\nAll relay routes timed out 30s\nGLMZ topology fragmented — traffic blocked',
        'ARP spoofing detected on 10.44.7.0/24\nGateway MAC changed 3x in 60s\nMan-in-the-middle attack suspected',
        'DNS hijack: glmz.relay.7 → 10.0.0.1\nExpected 172.16.44.12 got 10.0.0.1\nPhishing redirect — connection refused',
        'SYN flood on port 8443: 48k pkt/s\nHalf-open connections=65535 — table full\nbci_daemon connection refused — service down',
        'BGP route withdrawal: 10.44.0.0/16\nGLMZ district 4 unreachable\nAll paths withdrawn — isolation detected',
        'Firewall rule collision: rule 44 vs rule 7\nOverlapping src/dst — policy undefined\nPacket dropped — connection failure',
        'SSL_CTX_new() returned NULL\nOpenSSL error: lib(20) reason(65)\nOut of memory — TLS unavailable',
        'x509 certificate chain incomplete\nMissing intermediate CA at depth 1\nPeer verification failed — connection closed',
        'OCSP responder timeout: 5000ms\nCannot verify revocation status\nCertificate rejected — paranoid policy',
        'Diffie-Hellman key too small: 512 bits\nMinimum 2048 required by policy\nHandshake aborted — weak parameters',
        'HMAC verification failed: message tampered\nExpected 0x3af7c2d1 got 0xa1b2c3d4\nReplay attack? — connection terminated',
        'AES-GCM tag mismatch — decryption failed\nCiphertext: 64 bytes at 0x5582c4a0\nData integrity compromised — discard',
        'Random number generator failure\n/dev/random blocked — entropy pool empty\nCryptographic operations suspended',
        'Kernel canary check failed: pid:9144\nExpected 0x3af7deadbeef1337\nStack corruption — process killed',
        'ASLR bypass detected: same load addr 5x\nBase 0x5582c4a0 repeated across forks\nEntropy source failure — security degraded',
        'Control flow integrity violation\nIndirect call to 0x4141414141\nShadow stack mismatch — process terminated',
        'Spectre v2: indirect branch poisoning\nBranch predictor contamination detected\nProc 8812 isolation required — performance hit',
        'Meltdown mitigation disabled: KPTI=off\nKernel mapping exposed in user space\nSystem vulnerable — reboot required',
        'BCI foreign signature: employer key mismatch\nWeapon BIM expects corp_key=0x3301\nLockout enforced — weapon inoperable',
        'BCI motor cortex desync: 12k events lost\nBuffer overflow in neural_rx pipeline\nCalibration state corrupted — reset required',
        'BCI denial-of-sensation attack detected\nInput signal 0xFFFF flooding grip classifier\nOperator sensory loop disrupted — threat',
        'BCI lateral injection: foreign grip pattern\nClassifier trained on 0x3301 profile\nIntent classification unreliable — unsafe',
        'BCI key derivation failed: PBKDF2 abort\nSalt corrupted at byte 32 of 64\nNeural key unrecoverable — factory reset',
        'Corp policy violation: operator ID 3301\nUnauthorized district 4 access attempt\nAll credentials revoked — enforcement alerted',
        'Corp audit log gap: 300 entries missing\nTimestamp 2025-04-10T03:22 to T03:27\nLog tampering suspected — incident open',
        'Corp data exfiltration alert\n4.2MB outbound to 203.0.113.9 port 443\nDestination not in allowlist — blocked',
        'GLMZ enforcement beacon: lockdown active\nAll freelancer IDs suspended district 7\nbci_daemon forced offline by remote cmd',
        'GLMZ dark node exposure: IP 10.44.7.99\nNode appeared in GLMZ topology map\nOperator 3301 location triangulated',
        'GLMZ gate seizure: corp territorial acquisition\nDistrict 4 fabrication license revoked\nAll Crestfall BIM keys invalidated — mass lockout',
    ];

    // ── Floating code fragments ─────────────────────────────────────────────
    var FRAGS = [
        'for (int i = 0; i < n; i++) {\n    if (buf[i] == null) continue;\n    for (int j = 0; j < buf[i].Length; j++) {\n        if (!validator.Check(buf[i][j])) {\n            log.Warn($"invalid at [{i},{j}]");\n            fault.Record(i, j);\n            continue;\n        }\n        dispatch.Enqueue(buf[i][j]);\n    }\n    if (fault.Count > MAX_FAULTS) {\n        throw new IntegrityException(i);\n    }\n}',
        'while (!queue.IsEmpty()) {\n    var task = queue.Dequeue();\n    if (task.Priority == Priority.High) {\n        for (int r = 0; r < MAX_RETRY; r++) {\n            try {\n                await task.RunAsync(cts.Token);\n                break;\n            } catch (TimeoutException) {\n                if (r == MAX_RETRY - 1) throw;\n                await Task.Delay(backoff[r]);\n            }\n        }\n    } else {\n        pool.Submit(task);\n    }\n}',
        'switch (bci.IntentState) {\n    case IntentState.Precision:\n        if (rbs.CurrentPort != Port.Beta) {\n            await rbs.RotateAsync(Port.Beta);\n            led.Set(LedColor.Blue);\n            pin.Extend();\n        }\n        break;\n    case IntentState.Sweep:\n        if (rbs.CurrentPort != Port.Alpha) {\n            await rbs.RotateAsync(Port.Alpha);\n            led.Set(LedColor.Red);\n            pin.Retract();\n        }\n        break;\n    case IntentState.Transitioning:\n        led.Set(LedColor.Yellow);\n        await Task.Delay(RBS_ROTATION_MS);\n        break;\n    default:\n        if (mode != OverrideMode.Manual)\n            weapon.SafetyEngage();\n        break;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        for (int ring = 0; ring < CONTACT_RINGS; ring++) {\n            raw[ring] = adc.Sample(ring);\n            delta[ring] = raw[ring] - baseline[ring];\n            if (Math.Abs(delta[ring]) > NOISE_FLOOR) {\n                features[ring] = delta[ring] / baseline[ring];\n            } else {\n                features[ring] = 0f;\n            }\n        }\n        var intent = bim.Classify(features);\n        if (intent != lastIntent) {\n            rbs.RequestMode(intent);\n            lastIntent = intent;\n        }\n        await Task.Delay(POLL_INTERVAL_MS);\n    }\n});',
        'for (int epoch = 0; epoch < MAX_EPOCHS; epoch++) {\n    float acc = 0f;\n    for (int s = 0; s < samples.Count; s++) {\n        var pred = bim.Infer(samples[s].Features);\n        if (pred == samples[s].Label) acc++;\n    }\n    acc /= samples.Count;\n    if (acc >= TARGET_ACCURACY) {\n        bim.Commit();\n        log.Info($"cal done epoch={epoch} acc={acc:P1}");\n        break;\n    }\n    if (epoch == MAX_EPOCHS - 1) {\n        log.Warn("cal incomplete — defaulting");\n        bim.Revert();\n    }\n    bim.Adjust(lr * (1f - acc));\n}',
        'do {\n    var sig = cortex.Poll(POLL_TIMEOUT);\n    if (sig == null) { missedPolls++; continue; }\n    bim.Feed(sig);\n    if (bim.IsLocked) {\n        for (int i = 0; i < VERIFY_PASSES; i++) {\n            if (!bim.Verify(sig)) {\n                bim.Unlock();\n                missedPolls++;\n                break;\n            }\n        }\n    }\n    if (missedPolls > MAX_MISSED) {\n        log.Fatal("cortex signal lost");\n        throw new NeuralDesyncException();\n    }\n} while (!bim.IsLocked || missedPolls > 0);',
        'foreach (var node in graph.Nodes\n    .Where(n => n.District == targetDistrict)\n    .OrderBy(n => n.Latency)) {\n    if (node.IsBlacklisted) continue;\n    try {\n        var resp = await relay.PingAsync(node, PING_TIMEOUT);\n        if (resp.RTT < bestRTT) {\n            bestRTT  = resp.RTT;\n            bestNode = node;\n        }\n    } catch (TimeoutException) {\n        node.MissCount++;\n        if (node.MissCount > MAX_MISS) {\n            node.IsBlacklisted = true;\n            log.Warn($"node {node.Id} blacklisted");\n        }\n    }\n}',
        'for (int x = 0; x < W; x++) {\n    for (int y = 0; y < H; y++) {\n        float dx = Math.Min(x / (W * EDGE_FRAC), 1f);\n        float dy = Math.Min(y / (H * EDGE_FRAC), 1f);\n        float edge = Math.Min(dx, dy);\n        float n = fbm(\n            x / (float)W * NOISE_FREQ,\n            y / (float)H * NOISE_FREQ,\n            NOISE_OCTAVES);\n        float lo = n * TEAR_SCALE;\n        alpha[x, y] = Smoothstep(lo, lo + FEATHER, edge);\n        pixels[(y * W + x) * 4 + 3] =\n            (byte)(pixels[(y * W + x) * 4 + 3] * alpha[x, y]);\n    }\n}',
        'while (rbs.IsMoving) {\n    if (++elapsed > RBS_TIMEOUT_MS) {\n        rbs.EmergencyStop();\n        led.Set(LedColor.Dark);\n        switch (failMode) {\n            case FailMode.LockAlpha:\n                port = Port.Alpha;\n                break;\n            case FailMode.LockBeta:\n                port = Port.Beta;\n                break;\n            default:\n                weapon.SafetyEngage();\n                throw new RbsJamException(elapsed);\n        }\n        break;\n    }\n    await Task.Delay(1);\n}',
        'if (!enclave.Attest(expectedPcr)) {\n    log.Fatal("enclave attestation failed");\n    foreach (var key in vault.Keys.ToList()) {\n        try {\n            key.Revoke();\n            audit.Record(AuditEvent.KeyRevoked, key.Id);\n        } catch (Exception ex) {\n            log.Error($"revoke failed {key.Id}: {ex.Message}");\n        }\n    }\n    if (!lockdown.IsActive) {\n        lockdown.Engage(LockdownLevel.Hard);\n        await Task.Delay(LOCKDOWN_DELAY_MS);\n        net.Isolate();\n    }\n    Environment.Exit(ENCLAVE_FAIL_CODE);\n}',
        'Task.Run(async () => {\n    int failures = 0;\n    while (!cts.IsCancellationRequested) {\n        try {\n            await heartbeat.SendAsync(cts.Token);\n            failures = 0;\n        } catch (OperationCanceledException) {\n            break;\n        } catch (Exception ex) {\n            failures++;\n            log.Warn($"heartbeat fail #{failures}: {ex.Message}");\n            if (failures >= MAX_HB_FAILURES) {\n                log.Fatal("heartbeat dead — isolation");\n                net.Isolate();\n                return;\n            }\n        }\n        await Task.Delay(\n            failures == 0 ? INTERVAL_MS : INTERVAL_MS * (1 << failures),\n            cts.Token);\n    }\n});',
        'for (int page = 0; page < totalPages; page++) {\n    var buf = fs.ReadPage(page);\n    var actual   = Crc32.Compute(buf);\n    var expected = checksums[page];\n    if (actual != expected) {\n        corruptPages.Add(page);\n        log.Error($"page {page}: crc {actual:X8} != {expected:X8}");\n        if (corruptPages.Count > MAX_CORRUPT) {\n            throw new FilesystemCorruptException(\n                $"{corruptPages.Count} corrupt pages");\n        }\n        var recovered = journal.Recover(page);\n        if (recovered != null) {\n            fs.WritePage(page, recovered);\n            log.Info($"page {page} recovered from journal");\n        }\n    }\n}',
        'switch (pkType) {\n    case KeyType.Rsa4096:\n        if (!rsa.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("RSA-4096 invalid");\n        }\n        break;\n    case KeyType.EcdsaP256:\n        if (!ec.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("ECDSA-P256 invalid");\n        }\n        break;\n    case KeyType.Ed25519:\n        if (!ed.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("Ed25519 invalid");\n        }\n        break;\n    default:\n        audit.Alert(AuditEvent.UnknownKeyType, pkType);\n        throw new UnknownKeyTypeException(pkType);\n}',
        'while (wal.HasUnflushedFrames) {\n    var batch = wal.ReadBatch(CHECKPOINT_BATCH);\n    for (int i = 0; i < batch.Length; i++) {\n        db.ApplyFrame(batch[i]);\n        if (!db.VerifyFrame(batch[i])) {\n            log.Fatal($"WAL frame {batch[i].Lsn} corrupt");\n            db.RollbackTo(lastGoodLsn);\n            throw new WalCorruptException(batch[i].Lsn);\n        }\n        lastGoodLsn = batch[i].Lsn;\n    }\n    if (wal.FrameCount % CHECKPOINT_INTERVAL == 0) {\n        await wal.CheckpointAsync();\n        log.Debug($"checkpoint lsn={lastGoodLsn:X}");\n    }\n}',
        'foreach (var hop in route.Hops) {\n    if (enforcement.IsBlacklisted(hop.District)) {\n        log.Warn($"hop district {hop.District} blacklisted");\n        var alt = router.FindAlternate(hop, route.Destination);\n        if (alt == null) {\n            throw new NoRouteException(route.Destination);\n        }\n        route.Replace(hop, alt);\n        continue;\n    }\n    for (int attempt = 0; attempt < FORWARD_RETRIES; attempt++) {\n        if (relay.Forward(packet, hop)) break;\n        if (attempt == FORWARD_RETRIES - 1) {\n            hop.FailCount++;\n            route.Remove(hop);\n        }\n        await Task.Delay(RETRY_DELAY_MS << attempt);\n    }\n}',
        'do {\n    var chunk = heap.TryAlloc(size, alignment);\n    if (chunk != null) {\n        if (zeroed) chunk.Zero();\n        return chunk;\n    }\n    switch (oomPolicy) {\n        case OomPolicy.Collect:\n            gc.Collect(GcGeneration.All);\n            break;\n        case OomPolicy.Compact:\n            heap.Compact();\n            break;\n        case OomPolicy.Fail:\n            throw new OutOfMemoryException(\n                $"alloc {size}B align={alignment} failed");\n    }\n    retries++;\n} while (retries < MAX_ALLOC_RETRIES);',
        'for (int shard = 0; shard < vault.ShardCount; shard++) {\n    var s = vault.GetShard(shard);\n    if (!s.Verify(masterKey)) {\n        log.Warn($"shard {shard} verify failed — skipping");\n        badShards++;\n        continue;\n    }\n    unlockedShards++;\n    xorBuf.XorWith(s.KeyMaterial);\n    if (unlockedShards >= vault.Threshold) {\n        derivedKey = kdf.Derive(xorBuf, salt, KDF_ITERATIONS);\n        log.Info($"vault unsealed: {unlockedShards}/{vault.ShardCount}");\n        break;\n    }\n}\nif (unlockedShards < vault.Threshold) {\n    throw new VaultSealException(\n        $"only {unlockedShards} of {vault.Threshold} shards");\n}',
        'while (jit.HasPendingWork) {\n    var fn = jit.DequeueHotFunction();\n    if (fn.CallCount < JIT_THRESHOLD) continue;\n    try {\n        var ir  = lifter.Lift(fn.Bytecode);\n        var opt = optimizer.Run(ir, OptLevel.O2);\n        var mc  = emitter.Emit(opt);\n        jit.Install(fn, mc);\n        stats.JitCompiles++;\n    } catch (CompileException ex) {\n        log.Warn($"jit fail {fn.Name}: {ex.Message}");\n        fn.JitBlacklisted = true;\n    }\n    if (jit.CodeCacheUsed > JIT_CACHE_LIMIT) {\n        jit.EvictCold(EVICT_FRACTION);\n        log.Debug($"jit evict: cache={jit.CodeCacheUsed}B");\n    }\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        sig = Syscall.Poll(fds, POLL_TIMEOUT);\n        if (sig < 0) {\n            if (sig == EINTR) continue;\n            throw new SyscallException("poll", sig);\n        }\n        foreach (var fd in fds.Where(f => f.IsReady)) {\n            switch (fd.Events) {\n                case PollEvent.Read:\n                    var data = fd.Read();\n                    if (data.Length == 0) { fd.Close(); break; }\n                    handler.OnData(fd, data);\n                    break;\n                case PollEvent.HangUp:\n                    handler.OnClose(fd);\n                    fd.Close();\n                    break;\n                case PollEvent.Error:\n                    handler.OnError(fd);\n                    break;\n            }\n        }\n    }\n});',
        'for (int i = 0; i < contactRings; i++) {\n    raw    = adc.Read(i);\n    delta  = raw - baseline[i];\n    if (Math.Abs(delta) <= noiseFl) {\n        features[i] = 0f;\n        continue;\n    }\n    features[i] = delta / (float)baseline[i];\n    if (features[i] > 0 && !seenPositive) {\n        seenPositive   = true;\n        precisionScore += features[i];\n    } else if (features[i] < 0) {\n        sweepScore -= features[i];\n    }\n}\nvar result =\n    precisionScore > sweepScore + HYSTERESIS\n        ? GripIntent.Precision\n        : sweepScore > precisionScore + HYSTERESIS\n            ? GripIntent.Sweep\n            : lastIntent;',
        'while (relay.BackoffMs < MAX_BACKOFF_MS) {\n    try {\n        var conn = await relay.ConnectAsync(\n            dist12, relay.BackoffMs, cts.Token);\n        if (conn.IsAuthenticated) {\n            log.Info($"relay up latency={conn.RTT}ms");\n            relay.BackoffMs = BASE_BACKOFF_MS;\n            return conn;\n        }\n        log.Warn("relay auth rejected — backoff");\n    } catch (OperationCanceledException) {\n        throw;\n    } catch (Exception ex) {\n        log.Warn($"relay fail: {ex.Message}");\n    }\n    relay.BackoffMs = Math.Min(\n        relay.BackoffMs * 2, MAX_BACKOFF_MS);\n    await Task.Delay(relay.BackoffMs, cts.Token);\n}\nthrow new RelayUnreachableException(dist12);',
        'if (oom.Score(proc) > OOM_THRESHOLD) {\n    var victims = procs\n        .Where(p => p.OomAdj >= 0)\n        .OrderByDescending(p => p.OomScore)\n        .Take(MAX_VICTIMS)\n        .ToList();\n    foreach (var v in victims) {\n        log.Warn($"oom kill pid:{v.Pid} ({v.Name}) rss={v.Rss}K");\n        v.Kill(Signal.SIGKILL);\n        freed += v.Rss;\n        if (freed >= needed) break;\n    }\n    if (freed < needed) {\n        log.Fatal("oom: insufficient memory after kills");\n        kernel.Panic("Out of memory");\n    }\n}',
        'for (int b = 0; b < wal.BlockCount; b++) {\n    var blk = wal.ReadBlock(b);\n    db.ApplyBlock(blk);\n    if (b % WAL_CHECKPOINT_EVERY == 0) {\n        wal.Checkpoint();\n        db.Sync();\n        log.Debug($"wal checkpoint @{b}/{wal.BlockCount}");\n    }\n    if (!db.IsConsistent()) {\n        log.Error($"inconsistency after block {b}");\n        db.RollbackToSnapshot(lastSnapshot);\n        wal.Truncate(b);\n        break;\n    }\n}',
        // 100 more
        'Task.Run(async () => {\n    await foreach (var evt in stream.ReadAllAsync(cts.Token)) {\n        switch (evt.Type) {\n            case EventType.GripUpdate:\n                bim.Feed(evt.Payload);\n                break;\n            case EventType.Disconnect:\n                bci.Reconnect();\n                break;\n            case EventType.Calibrate:\n                await bim.RecalibrateAsync();\n                break;\n            default:\n                log.Warn($"unhandled event {evt.Type}");\n                break;\n        }\n    }\n});',
        'for (int attempt = 0; attempt < MAX_RETRIES; attempt++) {\n    try {\n        var result = await gate.AuthorizeAsync(\n            token, scope, cts.Token);\n        if (result.IsGranted) {\n            session.Open(result.Claims);\n            audit.Log(AuditEvent.Login, token.Subject);\n            return result;\n        }\n        log.Warn($"auth denied: {result.Reason}");\n        await Task.Delay(DENY_COOLDOWN_MS);\n    } catch (TokenExpiredException) {\n        token = await token.RefreshAsync();\n    } catch (GateOfflineException) {\n        if (attempt == MAX_RETRIES - 1) throw;\n        await Task.Delay(OFFLINE_BACKOFF_MS << attempt);\n    }\n}\nthrow new AuthFailedException(MAX_RETRIES);',
        'while (net.HasPendingFrames) {\n    var frame = net.Dequeue();\n    if (!tls.Verify(frame)) {\n        dropped++;\n        if (dropped > MAX_DROPS) {\n            log.Error("too many invalid frames — closing");\n            net.Close();\n            break;\n        }\n        continue;\n    }\n    switch (frame.Type) {\n        case FrameType.Data:\n            rx.Append(frame.Payload);\n            break;\n        case FrameType.Ack:\n            tx.Acknowledge(frame.Seq);\n            break;\n        case FrameType.Reset:\n            net.Reset();\n            rx.Clear(); tx.Clear();\n            break;\n    }\n}',
        'foreach (var cert in chain) {\n    if (cert.NotAfter < DateTime.UtcNow) {\n        throw new CertExpiredException(\n            $"{cert.Subject} expired {cert.NotAfter}");\n    }\n    if (revoked.Contains(cert.Thumbprint)) {\n        throw new CertRevokedException(cert.Subject);\n    }\n    if (!prev.PublicKey.VerifySignature(\n            cert.TbsBytes, cert.Signature)) {\n        throw new ChainBrokenException(\n            $"sig invalid at depth {depth}");\n    }\n    depth++;\n    prev = cert;\n}',
        'for (int gen = GcGeneration.Young;\n     gen <= GcGeneration.Old; gen++) {\n    var roots = gc.ScanRoots(gen);\n    foreach (var root in roots) {\n        gc.Mark(root);\n    }\n    var swept = gc.Sweep(gen);\n    log.Debug(\n        $"gc gen{gen}: marked={gc.Marked} swept={swept}B");\n    if (gen == GcGeneration.Old &&\n        heap.Fragmentation > COMPACT_THRESHOLD) {\n        heap.Compact();\n        log.Info("heap compacted after full gc");\n    }\n}',
        'while (!shutdown.IsRequested) {\n    var batch = await ingest.ReadBatchAsync(\n        BATCH_SIZE, cts.Token);\n    if (batch.Count == 0) {\n        await Task.Delay(IDLE_DELAY_MS);\n        continue;\n    }\n    for (int i = 0; i < batch.Count; i++) {\n        try {\n            await processor.HandleAsync(batch[i]);\n            metrics.Processed++;\n        } catch (RetryableException ex) {\n            log.Warn($"retry item {i}: {ex.Message}");\n            await ingest.RequeueAsync(batch[i]);\n        } catch (PoisonException) {\n            log.Error($"poison item {i} — discarding");\n            metrics.Poisoned++;\n        }\n    }\n}',
        'if (bci.IsLinked && !manual.IsActive) {\n    var raw = new float[CONTACT_RINGS];\n    for (int r = 0; r < CONTACT_RINGS; r++) {\n        raw[r] = adc.Sample(r);\n    }\n    var intent = bim.Classify(raw);\n    if (intent != prev && confidence >= MIN_CONF) {\n        switch (intent) {\n            case GripIntent.Precision:\n                await rbs.SelectAsync(Port.Beta);\n                led.Set(Blue); pin.Extend();\n                break;\n            case GripIntent.Sweep:\n                await rbs.SelectAsync(Port.Alpha);\n                led.Set(Red); pin.Retract();\n                break;\n        }\n        prev = intent;\n    }\n}',
        'Task.Run(async () => {\n    using var timer = new PeriodicTimer(\n        TimeSpan.FromMilliseconds(SCHED_INTERVAL));\n    while (await timer.WaitForNextTickAsync(cts.Token)) {\n        for (int i = 0; i < workers.Count; i++) {\n            if (!workers[i].IsIdle) continue;\n            var job = scheduler.Dequeue(\n                workers[i].Affinity);\n            if (job == null) continue;\n            await workers[i].RunAsync(job);\n            metrics.Dispatched++;\n        }\n        if (scheduler.Depth > OVERFLOW_THRESHOLD) {\n            log.Warn($"queue depth={scheduler.Depth}");\n            await workers.ScaleAsync(+SCALE_STEP);\n        }\n    }\n});',
        'for (int row = 0; row < db.PageCount; row++) {\n    var page = db.ReadPage(row);\n    if (page.Magic != DB_MAGIC) {\n        log.Error($"bad magic page {row}: {page.Magic:X}");\n        corrupt.Add(row);\n        continue;\n    }\n    for (int slot = 0; slot < page.SlotCount; slot++) {\n        var rec = page.ReadSlot(slot);\n        if (!rec.IsAlive) continue;\n        if (idx.Contains(rec.Key)) {\n            log.Warn($"duplicate key {rec.Key} at p{row}s{slot}");\n            duplicates++;\n        } else {\n            idx.Add(rec.Key, new Pointer(row, slot));\n        }\n    }\n}',
        'while (jit.TraceBuf.Count > 0) {\n    var trace = jit.TraceBuf.Dequeue();\n    if (trace.HotCount < TRACE_HOT) continue;\n    try {\n        var ir   = tracer.BuildIR(trace);\n        var ssa  = ssa.Convert(ir);\n        var opt1 = dce.Run(ssa);\n        var opt2 = gvn.Run(opt1);\n        var opt3 = licm.Run(opt2);\n        var mc   = regAlloc.Allocate(opt3);\n        var ptr  = emitter.Emit(mc);\n        jit.PatchTrace(trace.EntryAddr, ptr);\n    } catch (TraceAbortException ex) {\n        log.Debug($"trace abort: {ex.Reason}");\n        jit.Blacklist(trace.EntryAddr);\n    }\n}',
        'foreach (var edge in graph.Edges\n    .Where(e => !e.IsValid(DateTime.UtcNow))\n    .ToList()) {\n    graph.RemoveEdge(edge);\n    var src  = graph.GetNode(edge.SourceId);\n    var dst  = graph.GetNode(edge.TargetId);\n    if (src != null) src.Degree--;\n    if (dst != null) dst.Degree--;\n    if (src?.Degree == 0 && src.IsPrunable) {\n        graph.RemoveNode(src.Id);\n        pruned.Nodes++;\n    }\n    pruned.Edges++;\n}\nlog.Info($"graph pruned: {pruned.Nodes}n {pruned.Edges}e");',
        'do {\n    token = await oauth.RequestAsync(\n        clientId, scope, cts.Token);\n    if (token.IsValid) break;\n    log.Warn($"token invalid: {token.Error}");\n    if (token.Error == OAuthError.Revoked) {\n        await oauth.RevokeAsync(token);\n        throw new AuthRevokedException();\n    }\n    retries++;\n    if (retries >= MAX_TOKEN_RETRIES) {\n        throw new TokenExhaustedException();\n    }\n    await Task.Delay(\n        TOKEN_BACKOFF_MS * (1 << retries));\n} while (!token.IsValid);',
        'for (int pass = 0; pass < ENCRYPT_PASSES; pass++) {\n    for (int blk = 0; blk < data.BlockCount; blk++) {\n        var plain  = data.ReadBlock(blk);\n        var iv     = rng.NextBytes(IV_LEN);\n        var cipher = aes.Encrypt(plain, iv, key);\n        var tag    = hmac.Sign(cipher, authKey);\n        out.WriteBlock(blk, iv, cipher, tag);\n        if (blk % PROGRESS_INTERVAL == 0) {\n            progress?.Report(\n                (pass * data.BlockCount + blk) /\n                (float)(ENCRYPT_PASSES * data.BlockCount));\n        }\n    }\n    key = kdf.Derive(key, salt, 1);\n}',
        'while (socket.State == WebSocketState.Open) {\n    var result = await socket.ReceiveAsync(\n        buffer, cts.Token);\n    if (result.MessageType == Close) {\n        await socket.CloseAsync(\n            WebSocketCloseStatus.NormalClosure,\n            "bye", cts.Token);\n        break;\n    }\n    accumulated.Write(\n        buffer.Array, 0, result.Count);\n    if (result.EndOfMessage) {\n        var msg = Encoding.UTF8.GetString(\n            accumulated.ToArray());\n        await dispatcher.HandleAsync(msg);\n        accumulated.SetLength(0);\n    }\n}',
        'Task.Run(async () => {\n    var sema = new SemaphoreSlim(\n        MAX_CONCURRENT_OPS);\n    var tasks = items.Select(async item => {\n        await sema.WaitAsync(cts.Token);\n        try {\n            await processor.ProcessAsync(\n                item, cts.Token);\n            Interlocked.Increment(ref done);\n        } catch (Exception ex) {\n            log.Error(\n                $"process {item.Id}: {ex.Message}");\n            Interlocked.Increment(ref failed);\n        } finally {\n            sema.Release();\n        }\n    });\n    await Task.WhenAll(tasks);\n    log.Info($"done={done} failed={failed}");\n});',
        'for (int i = 0; i < bitmap.Width; i++) {\n    for (int j = 0; j < bitmap.Height; j++) {\n        int idx = (j * bitmap.Stride + i * 4);\n        float lum = (\n            pixels[idx + 0] * 0.2126f +\n            pixels[idx + 1] * 0.7152f +\n            pixels[idx + 2] * 0.0722f) / 255f;\n        float alpha = tornEdge.Sample(i, j);\n        pixels[idx + 3] = (byte)(\n            pixels[idx + 3] * alpha *\n            (0.6f + lum * 0.4f));\n    }\n}',
        'switch (packet.Protocol) {\n    case Protocol.Tcp:\n        if (!tcp.IsValidChecksum(packet)) {\n            dropped++; break;\n        }\n        switch (tcp.Flags) {\n            case TcpFlags.Syn:\n                connTable.OpenHalf(packet);\n                break;\n            case TcpFlags.SynAck:\n                connTable.Complete(packet);\n                break;\n            case TcpFlags.Fin:\n            case TcpFlags.Rst:\n                connTable.Close(packet);\n                break;\n        }\n        break;\n    case Protocol.Udp:\n        udp.Route(packet);\n        break;\n    case Protocol.Icmp:\n        if (firewall.AllowIcmp) icmp.Handle(packet);\n        break;\n}',
        'while (compressor.HasInput) {\n    var block = compressor.ReadInput(BLOCK_SIZE);\n    var lz4   = Lz4.Compress(block);\n    if (lz4.Length >= block.Length) {\n        out.WriteUncompressed(block);\n        stats.Uncompressed += block.Length;\n    } else {\n        out.WriteCompressed(lz4);\n        stats.Compressed   += block.Length;\n        stats.Saved        += block.Length - lz4.Length;\n    }\n    if (out.Position % FLUSH_INTERVAL == 0) {\n        await out.FlushAsync();\n    }\n}\nlog.Info($"ratio={stats.Ratio:F2} saved={stats.Saved}B");',
        'foreach (var proc in procTable.Values\n    .Where(p => p.State == ProcState.Zombie)\n    .ToList()) {\n    if (!proc.Parent.IsAlive) {\n        proc.Reparent(init);\n    }\n    var exit = proc.Reap();\n    log.Debug(\n        $"reaped pid:{proc.Pid} exit:{exit.Code}");\n    if (exit.Signal != Signal.None) {\n        log.Warn(\n            $"pid:{proc.Pid} killed by {exit.Signal}");\n    }\n    procTable.Remove(proc.Pid);\n    resources.Release(proc);\n}',
        'for (int lvl = 0; lvl < BTREE_LEVELS; lvl++) {\n    var node = btree.ReadNode(cursor.NodeId);\n    if (node.IsLeaf) {\n        var slot = node.FindSlot(key);\n        if (slot < 0) return null;\n        return node.ReadValue(slot);\n    }\n    int child = -1;\n    for (int k = 0; k < node.KeyCount; k++) {\n        if (key.CompareTo(node.Keys[k]) < 0) {\n            child = node.Children[k];\n            break;\n        }\n    }\n    if (child < 0)\n        child = node.Children[node.KeyCount];\n    cursor.NodeId = child;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        var snap = metrics.Snapshot();\n        foreach (var (key, val) in snap) {\n            await telemetry.EmitAsync(\n                key, val, DateTime.UtcNow);\n        }\n        if (snap.TryGetValue("heap_rss", out var rss)\n            && rss > RSS_ALERT_THRESHOLD) {\n            await alerting.FireAsync(\n                Alert.HighMemory, rss);\n        }\n        if (snap.TryGetValue("error_rate", out var er)\n            && er > ERROR_RATE_THRESHOLD) {\n            await alerting.FireAsync(\n                Alert.HighErrorRate, er);\n        }\n        await Task.Delay(METRICS_INTERVAL_MS);\n    }\n});',
        'for (int i = 0; i < nodes.Count; i++) {\n    for (int j = i + 1; j < nodes.Count; j++) {\n        if (!graph.HasEdge(nodes[i], nodes[j])) {\n            continue;\n        }\n        float w = similarity.Compute(\n            nodes[i].Embedding,\n            nodes[j].Embedding);\n        if (w < PRUNE_THRESHOLD) {\n            graph.RemoveEdge(nodes[i], nodes[j]);\n            pruned++;\n        } else if (w > MERGE_THRESHOLD) {\n            candidates.Enqueue(\n                (nodes[i], nodes[j], w));\n        }\n    }\n}',
        'while (input.HasMore) {\n    var token = lexer.Next();\n    switch (token.Kind) {\n        case TokenKind.If:\n            ast.Push(new IfNode());\n            break;\n        case TokenKind.While:\n            ast.Push(new WhileNode());\n            break;\n        case TokenKind.For:\n            ast.Push(new ForNode());\n            break;\n        case TokenKind.LBrace:\n            ast.OpenBlock();\n            break;\n        case TokenKind.RBrace:\n            var block = ast.CloseBlock();\n            ast.Peek().AddChild(block);\n            break;\n        case TokenKind.Eof:\n            return ast.Root;\n    }\n}',
        'foreach (var region in memMap.Regions) {\n    if (!region.Flags.HasFlag(MemFlags.Write)) {\n        continue;\n    }\n    for (ulong addr = region.Base;\n         addr < region.Base + region.Size;\n         addr += PAGE_SIZE) {\n        var page = mem.ReadPage(addr);\n        if (page.IsDirty) {\n            var hash = sha256.Hash(page.Data);\n            if (hash != page.ExpectedHash) {\n                tampered.Add(addr);\n                log.Warn(\n                    $"page tamper @{addr:X16}");\n            }\n            page.ClearDirty();\n        }\n    }\n}',
        'for (int r = 0; r < MAX_RETRIES; r++) {\n    try {\n        using var tx = db.BeginTransaction(\n            IsolationLevel.Serializable);\n        var existing = tx.Find<Node>(id);\n        if (existing != null) {\n            existing.Update(payload);\n        } else {\n            tx.Insert(new Node(id, payload));\n        }\n        await tx.CommitAsync();\n        return;\n    } catch (SerializationConflict) {\n        if (r == MAX_RETRIES - 1) throw;\n        await Task.Delay(\n            TX_BACKOFF_MS * (1 << r));\n    }\n}',
        'Task.Run(async () => {\n    var ring = new RingBuffer<Event>(RING_SIZE);\n    while (!cts.IsCancellationRequested) {\n        int count = producer.Drain(ring);\n        for (int i = 0; i < count; i++) {\n            var ev = ring[i];\n            switch (ev.Category) {\n                case Category.Audit:\n                    await auditLog.WriteAsync(ev);\n                    break;\n                case Category.Metric:\n                    metrics.Record(ev);\n                    break;\n                case Category.Alert:\n                    await alerting.NotifyAsync(ev);\n                    break;\n            }\n        }\n        if (count == 0)\n            await Task.Yield();\n    }\n});',
        'while (fsm.State != State.Terminal) {\n    var input = fsm.NextInput();\n    switch (fsm.State) {\n        case State.Idle:\n            if (input.Is(Input.Connect))\n                fsm.Transition(State.Handshake);\n            break;\n        case State.Handshake:\n            if (!tls.Complete(input))\n                fsm.Transition(State.Error);\n            else\n                fsm.Transition(State.Active);\n            break;\n        case State.Active:\n            session.Process(input);\n            if (input.Is(Input.Close))\n                fsm.Transition(State.Closing);\n            break;\n        case State.Error:\n            log.Error($"fsm error: {input}");\n            fsm.Transition(State.Terminal);\n            break;\n    }\n}',
        'for (int oct = 0; oct < OCTAVES; oct++) {\n    float freq  = BASE_FREQ * MathF.Pow(2f, oct);\n    float amp   = BASE_AMP  * MathF.Pow(PERSIST, oct);\n    for (int y = 0; y < H; y++) {\n        for (int x = 0; x < W; x++) {\n            float nx = x * freq / W;\n            float ny = y * freq / H;\n            noise[y * W + x] +=\n                vnoise(nx, ny, seed + oct) * amp;\n        }\n    }\n}\nfor (int i = 0; i < W * H; i++) {\n    noise[i] = (noise[i] + 1f) * 0.5f;\n    noise[i] = MathF.Pow(noise[i], CONTRAST);\n}',
        'foreach (var tx in pending.OrderBy(t => t.Fee)) {\n    if (block.ByteSize + tx.Size > MAX_BLOCK_SIZE)\n        break;\n    if (utxo.IsDoubleSpend(tx)) {\n        log.Warn($"double spend tx {tx.Hash}");\n        continue;\n    }\n    foreach (var input in tx.Inputs) {\n        if (!utxo.Contains(input.OutPoint)) {\n            log.Warn($"missing utxo {input.OutPoint}");\n            goto nextTx;\n        }\n    }\n    utxo.Apply(tx);\n    block.AddTransaction(tx);\n    nextTx:;\n}',
        'Task.Run(async () => {\n    var limiter = new RateLimiter(\n        RATE_LIMIT, TimeSpan.FromSeconds(1));\n    await foreach (var req in requests\n        .ReadAllAsync(cts.Token)) {\n        if (!await limiter.TryAcquireAsync()) {\n            await req.RespondAsync(\n                StatusCode.TooManyRequests);\n            metrics.Throttled++;\n            continue;\n        }\n        var resp = await handler.HandleAsync(req);\n        await req.RespondAsync(resp);\n        metrics.Handled++;\n    }\n});',
        'for (int i = threadpool.MinThreads;\n     i <= threadpool.MaxThreads; i++) {\n    var w = new Worker(i, queue);\n    workers.Add(w);\n    w.Start();\n    if (queue.Depth < SCALE_DOWN_THRESHOLD\n        && i > threadpool.MinThreads) {\n        log.Debug($"pool: capped at {i} workers");\n        break;\n    }\n}\nlog.Info(\n    $"threadpool: {workers.Count} workers active");',
        'while (cts.IsCancellationRequested == false) {\n    var req = await listener.AcceptAsync(cts.Token);\n    _ = Task.Run(async () => {\n        try {\n            if (!rateLimit.Allow(req.RemoteEp)) {\n                await req.RejectAsync(429);\n                return;\n            }\n            var resp = await router.RouteAsync(req);\n            await req.SendAsync(resp);\n            access.Log(req, resp);\n        } catch (Exception ex) {\n            log.Error(\n                $"req {req.Id}: {ex.Message}");\n            await req.RejectAsync(500);\n        }\n    }, cts.Token);\n}',
        'for (int page = startPage;\n     page <= endPage; page++) {\n    if (snapshot.Bitmap.IsSet(page)) {\n        var data = cow.ReadVersion(page, version);\n        out.WritePage(page, data);\n    } else {\n        var data = db.ReadPage(page);\n        out.WritePage(page, data);\n        snapshot.Bitmap.Set(page);\n    }\n    if ((page - startPage) % FLUSH_PAGES == 0) {\n        await out.FlushAsync();\n        progress?.Report(\n            (page - startPage) /\n            (float)(endPage - startPage));\n    }\n}',
        'switch (sig.Value) {\n    case Signal.SIGINT:\n    case Signal.SIGTERM:\n        log.Info($"caught {sig} — graceful shutdown");\n        await server.StopAsync(GRACEFUL_TIMEOUT);\n        break;\n    case Signal.SIGHUP:\n        log.Info("SIGHUP — reloading config");\n        await config.ReloadAsync();\n        await server.ReconfigureAsync(config);\n        break;\n    case Signal.SIGUSR1:\n        log.Info("SIGUSR1 — rotating logs");\n        await logger.RotateAsync();\n        break;\n    case Signal.SIGSEGV:\n        log.Fatal("SIGSEGV — crash dump");\n        await core.DumpAsync();\n        Environment.FailFast("SIGSEGV");\n        break;\n}',
        'Task.Run(async () => {\n    var pending = new List<LogEntry>();\n    while (!cts.IsCancellationRequested) {\n        var entry = await logQueue\n            .ReadAsync(cts.Token);\n        pending.Add(entry);\n        if (pending.Count >= FLUSH_BATCH ||\n            (DateTime.UtcNow - pending[0].Time)\n                > FLUSH_INTERVAL) {\n            try {\n                await sink.WriteBatchAsync(pending);\n                pending.Clear();\n            } catch (Exception ex) {\n                log.Error(\n                    $"log flush failed: {ex.Message}");\n                await Task.Delay(RETRY_DELAY);\n            }\n        }\n    }\n    if (pending.Count > 0)\n        await sink.WriteBatchAsync(pending);\n});',
        'for (int seg = 0; seg < segs.Count; seg++) {\n    var s = segs[seg];\n    if (s.Compression == Codec.None) {\n        out.Write(s.Data);\n    } else if (s.Compression == Codec.Lz4) {\n        out.Write(Lz4.Decompress(\n            s.Data, s.UncompressedLen));\n    } else if (s.Compression == Codec.Zstd) {\n        out.Write(Zstd.Decompress(s.Data));\n    } else {\n        throw new UnknownCodecException(\n            s.Compression);\n    }\n    if (s.Checksum != Crc32.Compute(s.Data)) {\n        throw new SegmentCorruptException(seg);\n    }\n}',
        'while (!input.IsEof) {\n    int ch = input.Peek();\n    if (ch == \'/\') {\n        input.Advance();\n        if (input.Peek() == \'/\') {\n            while (!input.IsEol) input.Advance();\n        } else if (input.Peek() == \'*\') {\n            while (!input.IsEof) {\n                input.Advance();\n                if (prev == \'*\' && input.Peek() == \'/\') {\n                    input.Advance(); break;\n                }\n                prev = (char)input.Peek();\n            }\n        } else {\n            yield return Token(TokenKind.Slash);\n        }\n    } else {\n        yield return Scan(input);\n    }\n}',
        'foreach (var shard in ring\n    .GetPreferredNodes(key, REPLICAS)) {\n    var ok = false;\n    for (int t = 0; t < WRITE_TRIES; t++) {\n        try {\n            await shard.PutAsync(\n                key, value, ttl, cts.Token);\n            ok = true; break;\n        } catch (NodeUnavailableException) {\n            var next = ring.NextNode(shard);\n            if (next == null) break;\n            shard = next;\n        }\n    }\n    if (ok) acks++;\n    if (acks >= WRITE_QUORUM) return;\n}\nif (acks < WRITE_QUORUM)\n    throw new QuorumException(acks, WRITE_QUORUM);',
        'Task.Run(async () => {\n    await using var conn = await pool.RentAsync();\n    while (!cts.IsCancellationRequested) {\n        var changes = await cdc.PollAsync(\n            lastLsn, cts.Token);\n        foreach (var ch in changes) {\n            switch (ch.Op) {\n                case CdcOp.Insert:\n                    await sink.OnInsert(ch.Row);\n                    break;\n                case CdcOp.Update:\n                    await sink.OnUpdate(\n                        ch.OldRow, ch.Row);\n                    break;\n                case CdcOp.Delete:\n                    await sink.OnDelete(ch.Row);\n                    break;\n            }\n            lastLsn = ch.Lsn;\n        }\n    }\n});',
        'for (int level = 0;\n     level < lsm.LevelCount; level++) {\n    var files = lsm.GetLevel(level);\n    if (files.Count < lsm.LevelMaxFiles(level))\n        continue;\n    var merged = Sstable.Merge(files);\n    foreach (var f in files) {\n        lsm.Remove(f);\n        f.Delete();\n    }\n    if (level + 1 < lsm.LevelCount) {\n        lsm.AddToLevel(level + 1, merged);\n    } else {\n        lsm.AddNewLevel(merged);\n    }\n    log.Info(\n        $"compacted L{level}: {files.Count}→1");\n}',
        'while (reader.TryRead(out var span)) {\n    for (int i = 0; i < span.Length; i++) {\n        ref var sample = ref span[i];\n        if (sample.Value > CLIP_HIGH) {\n            sample.Value = CLIP_HIGH;\n            clipped++;\n        } else if (sample.Value < CLIP_LOW) {\n            sample.Value = CLIP_LOW;\n            clipped++;\n        }\n        sum += sample.Value;\n        sumSq += sample.Value * sample.Value;\n        count++;\n    }\n    if (count % STATS_INTERVAL == 0) {\n        float mean = sum / count;\n        float var_ = sumSq / count - mean * mean;\n        telemetry.Record("signal_var", var_);\n    }\n}',
        'foreach (var group in events\n    .GroupBy(e => e.Category)\n    .OrderByDescending(g => g.Count())) {\n    var agg = new Aggregate {\n        Category = group.Key,\n        Count    = group.Count(),\n        Total    = group.Sum(e => e.Value),\n        P50      = group.Percentile(e => e.Value, 50),\n        P99      = group.Percentile(e => e.Value, 99),\n    };\n    if (agg.P99 > SLA_THRESHOLD) {\n        await alerting.FireAsync(\n            Alert.SlaBreach, agg);\n    }\n    await metrics.EmitAsync(agg);\n}',
        'Task.Run(async () => {\n    var buf = new byte[RECV_BUF];\n    while (true) {\n        int n = await udp.ReceiveAsync(\n            buf, cts.Token);\n        if (n < HEADER_LEN) continue;\n        var hdr = Header.Parse(buf, n);\n        if (!hdr.IsValid) {\n            malformed++; continue;\n        }\n        if (seen.Contains(hdr.Nonce)) {\n            replays++; continue;\n        }\n        seen.Add(hdr.Nonce);\n        var plain = chacha.Decrypt(\n            buf, HEADER_LEN, n - HEADER_LEN,\n            hdr.Nonce, sessionKey);\n        if (plain == null) {\n            authFails++; continue;\n        }\n        await dispatcher.HandleAsync(plain);\n    }\n});',
        'for (int r = 0; r < rows; r++) {\n    for (int c = 0; c < cols; c++) {\n        float s = 0f;\n        for (int k = 0; k < K; k++) {\n            s += A[r * K + k] * B[k * cols + c];\n        }\n        C[r * cols + c] = s;\n    }\n    if (r % PROGRESS_ROWS == 0) {\n        float pct = r / (float)rows * 100f;\n        log.Debug(\n            $"matmul: {pct:F0}% ({r}/{rows})");\n    }\n}',
        'while (!drain.IsComplete) {\n    var slot = ring.TryAcquireSlot();\n    if (slot < 0) {\n        await Task.Yield();\n        continue;\n    }\n    try {\n        var item = source.Take(cts.Token);\n        ring.Write(slot, Serialize(item));\n        ring.Publish(slot);\n        produced++;\n    } catch (InvalidOperationException) {\n        ring.Release(slot);\n        drain.Signal();\n    } catch (OperationCanceledException) {\n        ring.Release(slot);\n        break;\n    }\n}',
        'foreach (var (node, depth) in bfs.Traverse(root)) {\n    if (depth > MAX_DEPTH) {\n        bfs.Prune(node);\n        continue;\n    }\n    switch (node.Type) {\n        case NodeType.Entity:\n            index.Add(node.Id, node);\n            break;\n        case NodeType.Relation:\n            graph.Link(\n                node.SourceId,\n                node.TargetId,\n                node.Weight);\n            break;\n        case NodeType.Aggregate:\n            stats.Accumulate(node);\n            break;\n    }\n    if (node.HasChildren)\n        bfs.Enqueue(node.Children, depth + 1);\n}',
        'Task.Run(async () => {\n    await using var cursor = db.OpenCursor(\n        table, scanOrder);\n    while (await cursor.MoveNextAsync()) {\n        var row = cursor.Current;\n        if (predicate(row)) {\n            buffer.Add(row);\n            if (buffer.Count >= BATCH_SIZE) {\n                await handler.HandleBatchAsync(\n                    buffer);\n                buffer.Clear();\n            }\n        }\n    }\n    if (buffer.Count > 0) {\n        await handler.HandleBatchAsync(buffer);\n    }\n    log.Info(\n        $"scan done: {cursor.Scanned} rows");\n});',
        'for (int i = 0; i < proof.Length; i++) {\n    if (i % 2 == 0) {\n        current = sha256.Hash(\n            Concat(current, proof[i]));\n    } else {\n        current = sha256.Hash(\n            Concat(proof[i], current));\n    }\n    if (current == null ||\n        current.Length != HASH_LEN) {\n        throw new MerkleInvalidException(\n            $"bad hash at depth {i}");\n    }\n}\nif (!current.SequenceEqual(root)) {\n    throw new MerkleProofException();\n}',
        'while (freeList.Count < MIN_FREE_PAGES) {\n    var victim = clock.NextVictim();\n    if (victim == null) break;\n    if (victim.IsPinned) {\n        clock.Skip(victim);\n        continue;\n    }\n    if (victim.IsDirty) {\n        await disk.WriteAsync(\n            victim.PageId, victim.Data);\n        dirty--;\n    }\n    bufferPool.Evict(victim);\n    freeList.Add(victim.Frame);\n    evictions++;\n}',
        'foreach (var pipeline in pipelines) {\n    var result = pipeline.Source;\n    foreach (var stage in pipeline.Stages) {\n        try {\n            result = await stage.ProcessAsync(\n                result, cts.Token);\n        } catch (StageException ex) {\n            if (stage.CanBypass) {\n                log.Warn(\n                    $"bypass {stage.Name}: {ex.Message}");\n                continue;\n            }\n            throw;\n        }\n    }\n    await pipeline.Sink.WriteAsync(result);\n}',
        'for (int gen = 0; gen < GENERATIONS; gen++) {\n    var selected = population\n        .OrderByDescending(c => c.Fitness)\n        .Take(ELITE_COUNT)\n        .ToList();\n    while (selected.Count < POP_SIZE) {\n        var p1 = tournament.Select(population);\n        var p2 = tournament.Select(population);\n        var child = crossover.Apply(p1, p2);\n        if (rng.NextDouble() < MUTATION_RATE) {\n            mutation.Apply(child);\n        }\n        selected.Add(child);\n    }\n    population = selected;\n    var best = population.Max(c => c.Fitness);\n    log.Debug($"gen {gen}: best={best:F4}");\n}',
        'Task.Run(async () => {\n    var debounce = new Debouncer(\n        TimeSpan.FromMilliseconds(DEBOUNCE_MS));\n    await foreach (var change in watcher\n        .WatchAsync(cts.Token)) {\n        debounce.Trigger(change.Path, async () => {\n            try {\n                await handler.OnChangeAsync(\n                    change);\n                log.Info(\n                    $"reloaded {change.Path}");\n            } catch (Exception ex) {\n                log.Error(\n                    $"reload fail: {ex.Message}");\n            }\n        });\n    }\n});',
        'while (backpressure.IsHighWatermark) {\n    var evicted = 0;\n    foreach (var entry in cache\n        .OrderBy(e => e.LastAccess)\n        .Take(EVICT_BATCH)) {\n        if (entry.IsPinned) continue;\n        cache.Remove(entry.Key);\n        evicted++;\n        if (!backpressure.IsHighWatermark)\n            break;\n    }\n    if (evicted == 0) {\n        log.Warn("cache pressure: nothing evictable");\n        await Task.Delay(BACKPRESSURE_WAIT_MS);\n    }\n}',
        'for (int seg = segStart; seg < segEnd; seg++) {\n    var hdr = elf.ReadSegmentHeader(seg);\n    if (hdr.Type != PT_LOAD) continue;\n    var data = elf.ReadSegment(seg);\n    var vaddr = hdr.VAddr + baseAddr;\n    mem.Map(vaddr, hdr.MemSize, hdr.Flags);\n    mem.Write(vaddr, data);\n    if (data.Length < hdr.MemSize) {\n        mem.Zero(\n            vaddr + data.Length,\n            hdr.MemSize - data.Length);\n    }\n    if (hdr.Flags.HasFlag(PF_X)) {\n        cache.FlushICache(vaddr, hdr.MemSize);\n    }\n}',
        'foreach (var bucket in histogram\n    .GetBuckets()\n    .OrderBy(b => b.LowerBound)) {\n    float density =\n        bucket.Count / (float)histogram.Total;\n    for (int col = 0;\n         col < (int)(density * MAX_COLS);\n         col++) {\n        row.Append(\'█\');\n    }\n    row.Append(\n        $" [{bucket.LowerBound:F2}–"\n        + $"{bucket.UpperBound:F2}]"\n        + $" n={bucket.Count}");\n    output.WriteLine(row.ToString());\n    row.Clear();\n}',
        'Task.Run(async () => {\n    using var scope = services.CreateScope();\n    var repo = scope.Get<INodeRepository>();\n    while (!cts.IsCancellationRequested) {\n        var stale = await repo.FindStaleAsync(\n            olderThan: DateTime.UtcNow - TTL,\n            limit: PURGE_BATCH);\n        if (stale.Count == 0) {\n            await Task.Delay(IDLE_PERIOD_MS);\n            continue;\n        }\n        foreach (var n in stale) {\n            await repo.ArchiveAsync(n);\n            purged++;\n        }\n        log.Info(\n            $"purged {stale.Count} nodes total={purged}");\n    }\n});',
        'for (int i = 0; i < seq.Count - 1; i++) {\n    int gap = seq[i + 1].Timestamp\n            - seq[i].Timestamp;\n    if (gap < MIN_GAP || gap > MAX_GAP) {\n        anomalies.Add(new Anomaly {\n            Index = i,\n            Expected = NOMINAL_GAP,\n            Actual   = gap,\n            Severity = gap > MAX_GAP\n                ? Severity.High\n                : Severity.Low\n        });\n        if (anomalies.Count > MAX_ANOMALIES) {\n            throw new AnomalyFloodException();\n        }\n    }\n}',
        // 100 new entries
        'for (int ring = 0; ring < CONTACT_RINGS; ring++) {\n    var imp = adc.MeasureImpedance(ring);\n    if (imp > MAX_IMPEDANCE_KOHM) {\n        log.Warn(\n            $"ring{ring} impedance {imp}kΩ — above spec");\n        bci.FlagRing(ring, RingFlag.HighImpedance);\n        if (imp > CRITICAL_IMPEDANCE_KOHM) {\n            bci.DisableRing(ring);\n            audit.Record(AuditEvent.RingDisabled, ring);\n        }\n    } else {\n        bci.ClearFlag(ring, RingFlag.HighImpedance);\n    }\n}',
        'await foreach (var sig in cortex\n    .StreamAsync(SAMPLE_RATE_HZ, cts.Token)) {\n    for (int b = 0; b < BAND_COUNT; b++) {\n        power[b] = bandFilter[b].Process(sig);\n        if (power[b] < BAND_POWER_MIN[b]) {\n            log.Warn(\n                $"band {b} power {power[b]:F3} low");\n        }\n    }\n    var feat = featureExtractor.Extract(power);\n    var intent = bim.Infer(feat);\n    if (intent.Confidence >= MIN_CONFIDENCE) {\n        await dispatcher.PostIntentAsync(intent);\n    }\n}',
        'for (int attempt = 0; attempt < RBS_MAX_RETRIES; attempt++) {\n    var pos = rbs.RequestRotation(\n        targetPort, RBS_TIMEOUT_MS);\n    if (pos == RotationResult.Locked) {\n        led.Set(targetPort == Port.Alpha\n            ? LedColor.Red : LedColor.Blue);\n        log.Info(\n            $"rbs locked {targetPort} {rbs.RotationMs}ms");\n        return;\n    }\n    log.Warn(\n        $"rbs rotation attempt {attempt}: {pos}");\n    if (pos == RotationResult.Jammed) {\n        rbs.EmergencyStop();\n        throw new RbsJamException(targetPort);\n    }\n    await Task.Delay(RBS_RETRY_DELAY_MS);\n}',
        'using var tx = db.BeginTransaction(\n    IsolationLevel.ReadCommitted);\ntry {\n    var node = new EntityNode {\n        Id       = Guid.NewGuid(),\n        District = operator.District,\n        Tags     = tags,\n        ValidFrom = DateTime.UtcNow,\n        ValidTo   = DateTime.MaxValue\n    };\n    tx.Insert(node);\n    foreach (var rel in relationships) {\n        tx.Insert(new EntityEdge(\n            node.Id, rel.TargetId, rel.Type));\n    }\n    await tx.CommitAsync();\n    graph.AddNode(node);\n    log.Info($"entity {node.Id} inserted");\n} catch {\n    tx.Rollback();\n    throw;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        var snap = hkb.GetStatus();\n        metrics.HkbPsi       = snap.Psi;\n        metrics.HkbFluidPct  = snap.FluidPercent;\n        metrics.HkbWearScore = snap.WearScore;\n        metrics.HkbCycles    = snap.CycleCount;\n        if (snap.WearScore > HKB_WARN_SCORE) {\n            log.Warn(\n                $"hkb wear {snap.WearScore:F2} — service soon");\n        }\n        if (snap.FluidPercent < HKB_FLUID_WARN_PCT) {\n            log.Warn(\n                $"hkb fluid {snap.FluidPercent}% — refill");\n        }\n        await Task.Delay(\n            HKB_MONITOR_INTERVAL_MS, cts.Token);\n    }\n});',
        'for (int i = 0; i < auditEvents.Count; i++) {\n    var ev = auditEvents[i];\n    if (!ev.Signature.Verify(\n            auditKey, ev.Payload)) {\n        log.Error(\n            $"audit event {ev.Id} sig invalid — possible tamper");\n        tampered.Add(ev);\n        continue;\n    }\n    if (i > 0 && ev.PrevHash != auditEvents[i-1].Hash) {\n        log.Error(\n            $"audit chain break at {ev.Id}");\n        gaps.Add((auditEvents[i-1].Id, ev.Id));\n    }\n    verified++;\n}\nif (tampered.Count > 0 || gaps.Count > 0) {\n    await sec.RaiseIncidentAsync(\n        IncidentType.AuditChainBroken);\n}',
        'foreach (var hop in glmz\n    .GetRelayPath(src, dst)\n    .Where(h => !enforcement.IsBlocked(h.NodeId))) {\n    if (hop.Latency > MAX_HOP_LATENCY_MS) {\n        log.Warn(\n            $"hop {hop.NodeId} latency {hop.Latency}ms");\n        var alt = glmz.FindAlternateHop(\n            hop, dst);\n        if (alt != null) {\n            route.Replace(hop, alt);\n            continue;\n        }\n    }\n    if (!hop.Authenticate(sessionKey)) {\n        throw new HopAuthException(hop.NodeId);\n    }\n    packet.Forward(hop);\n}',
        'while (!rbs.IsIdle) {\n    if (elapsed > RBS_IDLE_TIMEOUT_MS) {\n        log.Warn(\n            $"rbs idle timeout {elapsed}ms");\n        rbs.Reset();\n        break;\n    }\n    var state = rbs.PollState();\n    switch (state) {\n        case RbsState.Rotating:\n            led.Set(LedColor.Yellow);\n            break;\n        case RbsState.Detenting:\n            led.Blink(LedColor.Yellow, 100);\n            break;\n        case RbsState.Error:\n            log.Error("rbs error during idle wait");\n            throw new RbsStateException(state);\n    }\n    elapsed += POLL_MS;\n    await Task.Delay(POLL_MS);\n}',
        'for (int s = 0; s < shards.Count; s++) {\n    try {\n        var material = shards[s].Decrypt(\n            operatorKey);\n        xorAccum.XorWith(material);\n        unlocked++;\n        log.Info(\n            $"shard {s}/{shards.Count} unlocked");\n    } catch (DecryptException ex) {\n        log.Warn(\n            $"shard {s} decrypt fail: {ex.Message}");\n        failed.Add(s);\n    }\n    if (unlocked >= vault.Threshold) {\n        var vaultKey = kdf.Derive(\n            xorAccum, vault.Salt, KDF_ITERS);\n        vault.Unseal(vaultKey);\n        log.Info("vault unsealed");\n        return;\n    }\n}\nthrow new VaultSealException(unlocked, vault.Threshold);',
        'Task.Run(async () => {\n    var backoff = new ExponentialBackoff(\n        BASE_MS, MAX_MS);\n    while (!cts.IsCancellationRequested) {\n        try {\n            var token = await corp.AuthenticateAsync(\n                operatorId, neuralKey, cts.Token);\n            if (!token.IsValid) {\n                log.Warn(\n                    $"corp auth denied: {token.Reason}");\n                await Task.Delay(\n                    DENY_BACKOFF_MS, cts.Token);\n                continue;\n            }\n            session.Token = token;\n            audit.Log(AuditEvent.CorpAuth, operatorId);\n            backoff.Reset();\n        } catch (Exception ex) {\n            log.Error(\n                $"corp auth error: {ex.Message}");\n            await Task.Delay(\n                backoff.Next(), cts.Token);\n        }\n    }\n});',
        'foreach (var edge in graph\n    .GetEdges(nodeId, EdgeDirection.Outbound)\n    .Where(e => e.Type == EdgeType.Trust)) {\n    if (!edge.IsValid(DateTime.UtcNow)) {\n        graph.Expire(edge);\n        expired++;\n        continue;\n    }\n    var target = graph.GetNode(edge.TargetId);\n    if (target == null ||\n        target.Tags.Contains("blacklisted")) {\n        graph.RemoveEdge(edge.Id);\n        log.Warn(\n            $"trust edge {edge.Id} to blacklisted {edge.TargetId}");\n        continue;\n    }\n    trustPeers.Add(target);\n}',
        'for (int blk = 0; blk < cipher.BlockCount; blk++) {\n    var plain = source.ReadBlock(blk, BLOCK_SIZE);\n    var iv    = nonce.NextBytes(IV_SIZE);\n    var ct    = aes.EncryptGcm(\n        plain, iv, aad, key);\n    var tag   = aes.Tag;\n    dest.WriteBlock(blk, iv, ct, tag);\n    if (!aes.VerifyTag(ct, iv, aad, key, tag)) {\n        log.Fatal(\n            $"gcm tag mismatch at block {blk}");\n        throw new GcmTagException(blk);\n    }\n    if (blk % FLUSH_EVERY == 0) {\n        await dest.FlushAsync();\n    }\n}',
        'while (relay.IsConnected) {\n    var frame = await relay.RecvFrameAsync(\n        FRAME_TIMEOUT_MS, cts.Token);\n    if (!frame.VerifyHmac(sessionKey)) {\n        tampered++;\n        log.Warn(\n            $"relay frame hmac fail #{tampered}");\n        if (tampered > MAX_TAMPER) {\n            log.Error("relay channel compromised");\n            relay.Close();\n            break;\n        }\n        continue;\n    }\n    tampered = 0;\n    switch (frame.Type) {\n        case FrameType.Data:\n            await handler.HandleAsync(frame.Payload);\n            break;\n        case FrameType.Close:\n            relay.Close();\n            return;\n    }\n}',
        'for (int epoch = 0; epoch < bim.MaxEpochs; epoch++) {\n    var loss = 0f;\n    for (int b = 0; b < batches.Count; b++) {\n        var (x, y) = batches[b];\n        var pred   = bim.Forward(x);\n        var l      = crossEntropy(pred, y);\n        loss      += l;\n        bim.Backward(l, lr);\n        if ((b + 1) % GRAD_ACCUM == 0) {\n            bim.UpdateWeights();\n            bim.ZeroGrad();\n        }\n    }\n    loss /= batches.Count;\n    log.Debug(\n        $"bim epoch {epoch}: loss={loss:F4}");\n    if (loss < BIM_CONVERGE_THRESH) {\n        bim.Commit();\n        log.Info(\n            $"bim converged epoch={epoch}");\n        break;\n    }\n}',
        'Task.Run(async () => {\n    using var wg = new WireGuardTunnel(\n        dist12.Endpoint, localKey, peerKey);\n    await wg.HandshakeAsync(cts.Token);\n    log.Info(\n        $"wg tunnel up: {dist12.Endpoint}");\n    while (!cts.IsCancellationRequested) {\n        var pkt = await wg.RecvAsync(cts.Token);\n        if (pkt.IsKeepalive) {\n            metrics.WgKeepalives++;\n            continue;\n        }\n        await inner.SendAsync(\n            pkt.Payload, cts.Token);\n        metrics.WgFrames++;\n        if (metrics.WgFrames %\n            WG_REKEY_INTERVAL == 0) {\n            await wg.RekeyAsync(cts.Token);\n            log.Info("wg rekey ok");\n        }\n    }\n});',
        'foreach (var op in pendingOps\n    .Where(o => o.Uid == targetUid)\n    .OrderBy(o => o.Timestamp)) {\n    var allowed = authz.Check(\n        op.Uid, op.Resource, op.Action);\n    if (!allowed) {\n        audit.Deny(\n            op.Uid, op.Resource, op.Action);\n        op.Reject(new AuthzException(\n            op.Uid, op.Resource));\n        denied++;\n        continue;\n    }\n    try {\n        var r = await executor.RunAsync(\n            op, cts.Token);\n        op.Complete(r);\n        audit.Allow(\n            op.Uid, op.Resource, op.Action);\n        done++;\n    } catch (PolicyException ex) {\n        op.Fail(ex);\n        audit.PolicyViolation(op.Uid, ex.Rule);\n    }\n}',
        'for (int pass = 0; pass < 2; pass++) {\n    var targets = pass == 0\n        ? scanners.Where(s => s.Priority == High)\n        : scanners.Where(s => s.Priority != High);\n    foreach (var scanner in targets) {\n        var findings = await scanner\n            .ScanAsync(scope, cts.Token);\n        foreach (var f in findings) {\n            log.Warn(\n                $"sec scan: {f.Type} {f.Severity} @ {f.Location}");\n            siem.Ingest(f);\n            if (f.Severity >= Severity.High) {\n                await responder\n                    .RespondAsync(f, cts.Token);\n            }\n        }\n    }\n}',
        'while (wal.HasFrames) {\n    var frame = wal.ReadNextFrame();\n    if (!frame.ValidateChecksum()) {\n        log.Error(\n            $"wal frame {frame.Lsn} checksum fail");\n        corrupt.Add(frame.Lsn);\n        if (corrupt.Count > MAX_CORRUPT_FRAMES) {\n            throw new WalCorruptException(\n                corrupt.Count);\n        }\n        wal.Skip(frame);\n        continue;\n    }\n    await db.ApplyAsync(frame);\n    lastApplied = frame.Lsn;\n    if (lastApplied % CHECKPOINT_EVERY == 0) {\n        await wal.CheckpointAsync();\n        db.FlushPage();\n        log.Debug(\n            $"wal checkpoint @lsn={lastApplied:X}");\n    }\n}',
        'foreach (var proc in scheduler\n    .GetOverdueJobs(DateTime.UtcNow)\n    .Take(MAX_OVERDUE_BATCH)) {\n    if (proc.RetryCount >= MAX_RETRIES) {\n        log.Error(\n            $"job {proc.Id} max retries — dead letter");\n        deadLetter.Enqueue(proc);\n        scheduler.Remove(proc.Id);\n        continue;\n    }\n    proc.RetryCount++;\n    proc.NextRun = DateTime.UtcNow +\n        TimeSpan.FromMilliseconds(\n            RETRY_BASE_MS * (1 << proc.RetryCount));\n    scheduler.Reschedule(proc);\n    log.Warn(\n        $"job {proc.Id} retry #{proc.RetryCount}");\n}',
        'Task.Run(async () => {\n    var ring = new RingBuffer<BciSample>(\n        RING_CAPACITY);\n    await foreach (var s in bci\n        .StreamAsync(cts.Token)) {\n        if (!ring.TryWrite(s)) {\n            log.Warn("bci ring buffer full — drop");\n            metrics.Dropped++;\n            continue;\n        }\n        metrics.Received++;\n        if (ring.Count >= PROCESS_THRESHOLD) {\n            var batch = ring.ReadAll();\n            var feats = extractor.Extract(batch);\n            var intent = bim.Classify(feats);\n            if (intent != lastIntent) {\n                await rbs.RequestAsync(\n                    intent, cts.Token);\n                lastIntent = intent;\n            }\n        }\n    }\n});',
        'for (int i = 0; i < db.NodeCount; i++) {\n    var node = db.GetNodeAt(i);\n    var edges = db.GetEdges(node.Id);\n    var score = edges\n        .Where(e => e.Type == EdgeType.Trust)\n        .Sum(e => e.Weight);\n    node.TrustScore = score /\n        Math.Max(1, edges.Count);\n    if (node.TrustScore < TRUST_THRESHOLD) {\n        log.Warn(\n            $"node {node.Id} trust {node.TrustScore:F2}");\n        enforcement.Flag(node.Id);\n    }\n    db.UpdateNode(node);\n}',
        'while (cert.NotAfter < DateTime.UtcNow\n    .AddDays(RENEW_BEFORE_DAYS)) {\n    try {\n        var csr = certFactory.CreateCsr(\n            cert.Subject, cert.KeyType);\n        var newCert = await ca\n            .SignAsync(csr, CERT_VALIDITY_DAYS);\n        cert = newCert;\n        store.Update(cert);\n        log.Info(\n            $"cert renewed: {cert.Subject} exp={cert.NotAfter:d}");\n        break;\n    } catch (CaUnavailableException) {\n        log.Warn("CA unavailable — retry in 1h");\n        await Task.Delay(\n            TimeSpan.FromHours(1), cts.Token);\n    }\n}',
        'foreach (var candidate in graph\n    .Nodes\n    .Where(n => n.District == srcDistrict &&\n                n.IsActive &&\n                !enforcement.HasWarrant(n.Id))\n    .OrderByDescending(n => n.TrustScore)\n    .Take(TOP_K)) {\n    var ping = await relay\n        .PingAsync(candidate.RelayEndpoint,\n                   PING_TIMEOUT_MS);\n    if (ping.Success && ping.RTT < bestRTT) {\n        bestRTT  = ping.RTT;\n        bestNode = candidate;\n    }\n}\nif (bestNode == null) {\n    throw new NoRouteException(srcDistrict);\n}',
        'for (int gen = 0; gen < GC_GENS; gen++) {\n    var live = 0;\n    for (int i = 0; i < heap[gen].Count; i++) {\n        var obj = heap[gen][i];\n        if (!obj.IsMarked) {\n            obj.Finalize();\n            heap[gen].RemoveAt(i--);\n            freed += obj.Size;\n            continue;\n        }\n        obj.ClearMark();\n        live++;\n        if (gen + 1 < GC_GENS &&\n            obj.Age >= PROMOTE_AGE) {\n            heap[gen + 1].Add(obj);\n            heap[gen].RemoveAt(i--);\n        }\n    }\n    log.Debug(\n        $"gc gen{gen}: live={live} freed={freed}B");\n}',
        'Task.Run(async () => {\n    var monitor = new CertMonitor(\n        store, CHECK_INTERVAL_MS);\n    monitor.OnExpiring += async (cert) => {\n        log.Warn(\n            $"cert expiring: {cert.Subject} {cert.NotAfter:d}");\n        if (cert.AutoRenew) {\n            await renewer\n                .RenewAsync(cert, cts.Token);\n        } else {\n            await pager.AlertAsync(\n                Alert.CertExpiring, cert);\n        }\n    };\n    monitor.OnRevoked += async (cert) => {\n        log.Error(\n            $"cert revoked: {cert.Subject}");\n        await revoked.HandleAsync(cert);\n    };\n    await monitor.RunAsync(cts.Token);\n});',
        'for (int b = 0; b < tx.OpCount; b++) {\n    var op  = tx.ReadOp(b);\n    var old = db.Get(op.Key);\n    switch (op.Type) {\n        case OpType.Insert:\n            if (old != null)\n                throw new DuplicateKeyException(op.Key);\n            db.Put(op.Key, op.Value);\n            break;\n        case OpType.Update:\n            if (old == null)\n                throw new NotFoundException(op.Key);\n            db.Put(op.Key, op.Value);\n            undo.Push(\n                new UndoOp(op.Key, old));\n            break;\n        case OpType.Delete:\n            if (old == null) break;\n            db.Delete(op.Key);\n            undo.Push(\n                new UndoOp(op.Key, old));\n            break;\n    }\n}',
        'while (!enforcement.IsCleared(district)) {\n    var status = await enforcement\n        .QueryAsync(district, cts.Token);\n    switch (status.Level) {\n        case EnfLevel.None:\n            enforcement.MarkCleared(district);\n            break;\n        case EnfLevel.Watch:\n            log.Warn(\n                $"district {district} watch active");\n            await Task.Delay(\n                WATCH_POLL_MS, cts.Token);\n            break;\n        case EnfLevel.Lockdown:\n            log.Error(\n                $"district {district} lockdown!");\n            throw new LockdownException(district);\n    }\n}',
        'foreach (var rule in policy\n    .GetRules(uid)\n    .OrderByDescending(r => r.Priority)) {\n    if (!rule.AppliesTo(resource, action)) {\n        continue;\n    }\n    var ctx = new PolicyContext(\n        uid, resource, action,\n        DateTime.UtcNow);\n    var result = rule.Evaluate(ctx);\n    audit.Record(result, rule.Id, ctx);\n    if (result == PolicyResult.Deny) {\n        log.Info(\n            $"policy deny: rule {rule.Id} uid={uid}");\n        return false;\n    }\n    if (result == PolicyResult.Allow) {\n        return true;\n    }\n}',
        'Task.Run(async () => {\n    var enc = new AesGcmStream(\n        sessionKey, nonceGen);\n    await using var netStream =\n        await glmz.OpenDarkChannelAsync(\n            darkNode, cts.Token);\n    await using var encStream =\n        enc.Wrap(netStream);\n    await Pipeline.RunAsync(\n        source: localQueue,\n        sink: encStream,\n        transform: async (data) => {\n            metrics.Sent += data.Length;\n            return data;\n        },\n        ct: cts.Token);\n    log.Info(\n        $"dark channel closed: {darkNode}");\n});',
        'for (int i = 0; i < SIEVE_PASSES; i++) {\n    var candidates = events\n        .Where(e => e.Score > ANOMALY_MIN)\n        .OrderByDescending(e => e.Score)\n        .ToList();\n    foreach (var ev in candidates) {\n        var context = await enricher\n            .EnrichAsync(ev, cts.Token);\n        if (context.IsKnownBenign) {\n            ev.Score *= BENIGN_DECAY;\n            continue;\n        }\n        if (ev.Score > ALERT_THRESHOLD) {\n            await siem.IngestAsync(ev, context);\n            metrics.Alerts++;\n        }\n    }\n    events.RemoveAll(\n        e => e.Score < PURGE_THRESHOLD);\n}',
        'while (jit.PendingCount > 0) {\n    var fn = jit.PeekHottestFunction();\n    if (fn.CallCount < JIT_HOT_THRESHOLD)\n        break;\n    jit.Dequeue();\n    var span = Stopwatch.StartNew();\n    try {\n        var ir  = lifter.LiftFunction(fn);\n        var opt = optimizer.Optimize(ir,\n            OptPass.DCE |\n            OptPass.GVN |\n            OptPass.LICM);\n        var mc  = regAlloc.Allocate(opt);\n        jit.Install(fn.Id,\n            emitter.Emit(mc));\n        log.Debug(\n            $"jit {fn.Name} {span.ElapsedMs}ms");\n    } catch (CompileException ex) {\n        fn.Blacklist();\n        log.Warn(\n            $"jit fail {fn.Name}: {ex.Message}");\n    }\n}',
        'foreach (var seg in defrag\n    .GetFragmentedRegions()\n    .OrderBy(r => r.StartAddr)) {\n    var live = seg.LiveObjects\n        .OrderBy(o => o.Address)\n        .ToList();\n    for (int i = 0; i < live.Count; i++) {\n        var dest = seg.StartAddr +\n            (ulong)(i * ALIGN_SIZE);\n        if (live[i].Address == dest)\n            continue;\n        mem.Move(live[i].Address,\n            dest, live[i].Size);\n        live[i].UpdateReference(dest);\n        log.Debug(\n            $"move {live[i].Address:X} → {dest:X}");\n    }\n    freed += seg.Size - live.Count * ALIGN_SIZE;\n}',
        'Task.Run(async () => {\n    var reconn = 0;\n    while (!cts.IsCancellationRequested) {\n        try {\n            using var ws = await wsFactory\n                .ConnectAsync(endpoint, cts.Token);\n            reconn = 0;\n            log.Info(\n                $"ws connected: {endpoint}");\n            await ws.ServeAsync(\n                dispatcher, cts.Token);\n        } catch (WebSocketException ex)\n            when (!cts.IsCancellationRequested) {\n            reconn++;\n            var delay = Math.Min(\n                WS_BASE_MS * (1 << reconn),\n                WS_MAX_MS);\n            log.Warn(\n                $"ws error #{reconn}: {ex.Message} — retry {delay}ms");\n            await Task.Delay(delay, cts.Token);\n        }\n    }\n});',
        'for (int row = 0; row < result.RowCount; row++) {\n    var r = result.GetRow(row);\n    if (r.IsNull("district")) continue;\n    var district = r.GetString("district");\n    var lat      = r.GetFloat("latency_ms");\n    var ts       = r.GetDateTime("timestamp");\n    if (!latencyByDistrict\n            .TryGetValue(district, out var hist)) {\n        hist = new Histogram(HIST_BUCKETS);\n        latencyByDistrict[district] = hist;\n    }\n    hist.Record(lat);\n    if (lat > SLA_THRESHOLD_MS) {\n        violations.Add(\n            new SlaViolation(district, lat, ts));\n    }\n}',
        'while (proc.IsRunning) {\n    var usage = await proc\n        .GetResourceUsageAsync();\n    if (usage.CpuPercent > CPU_THROTTLE_PCT) {\n        await cgroup.SetCpuQuota(\n            proc.CgroupPath,\n            CPU_THROTTLE_QUOTA);\n        log.Warn(\n            $"pid:{proc.Pid} cpu throttled {usage.CpuPercent}%");\n    } else if (usage.CpuPercent < CPU_RELEASE_PCT) {\n        await cgroup.RemoveCpuQuota(\n            proc.CgroupPath);\n    }\n    if (usage.RssKb > RSS_LIMIT_KB) {\n        log.Error(\n            $"pid:{proc.Pid} rss {usage.RssKb}K — OOM risk");\n        await alerting.FireAsync(\n            Alert.HighMemory, proc.Pid);\n    }\n    await Task.Delay(MONITOR_MS, cts.Token);\n}',
        'foreach (var node in glmz\n    .GetDistrictNodes(district)\n    .Where(n => n.LastSeen <\n        DateTime.UtcNow - STALE_THRESHOLD)) {\n    var probe = await net\n        .ProbeAsync(node.Endpoint,\n                    PROBE_TIMEOUT_MS);\n    if (!probe.IsReachable) {\n        node.MissCount++;\n        log.Warn(\n            $"node {node.Id} miss #{node.MissCount}");\n        if (node.MissCount >= DEAD_THRESHOLD) {\n            glmz.MarkDead(node.Id);\n            await topology\n                .RemoveAsync(node.Id);\n            log.Info(\n                $"node {node.Id} removed");\n        }\n    } else {\n        node.MissCount = 0;\n        node.LastSeen  = DateTime.UtcNow;\n    }\n}',
        'Task.Run(async () => {\n    await using var chan =\n        await corp.OpenEnclaveChannelAsync(\n            enclaveId, cts.Token);\n    while (!cts.IsCancellationRequested) {\n        var req = await chan.ReadRequestAsync(\n            cts.Token);\n        switch (req.Type) {\n            case EnclaveReq.Attest:\n                var report = enclave.GetReport();\n                await chan.ReplyAsync(\n                    report, cts.Token);\n                break;\n            case EnclaveReq.Derive:\n                var key = kdf.Derive(\n                    masterSeed, req.Context,\n                    KDF_ITERS);\n                await chan.ReplyAsync(\n                    key, cts.Token);\n                break;\n            case EnclaveReq.Seal:\n                var sealed2 = enclave.Seal(\n                    req.Payload);\n                await chan.ReplyAsync(\n                    sealed2, cts.Token);\n                break;\n        }\n    }\n});',
        'for (int i = 0; i < ops.Count; i++) {\n    if (!rateLimit.TryAcquire(\n            ops[i].Uid, 1)) {\n        log.Warn(\n            $"rate limit uid={ops[i].Uid}");\n        ops[i].Reject(\n            new RateLimitException(ops[i].Uid));\n        limited++;\n        continue;\n    }\n    var result = await dispatcher\n        .DispatchAsync(ops[i], cts.Token);\n    if (!result.Success) {\n        log.Error(\n            $"op {ops[i].Id} fail: {result.Error}");\n        failed.Add(ops[i].Id);\n        continue;\n    }\n    completed++;\n}',
        'while (bci.IsLinked) {\n    var signals = new float[CONTACT_RINGS];\n    for (int r = 0; r < CONTACT_RINGS; r++) {\n        signals[r] = bci.ReadRing(r);\n        if (float.IsNaN(signals[r])) {\n            log.Warn(\n                $"bci ring{r} NaN — artifact");\n            signals[r] = 0f;\n            artifacts++;\n        }\n    }\n    if (artifacts > MAX_ARTIFACTS_PER_WINDOW) {\n        log.Error("artifact rate exceeded — skip epoch");\n        artifacts = 0;\n        continue;\n    }\n    var feat   = extractor.Extract(signals);\n    var intent = bim.Infer(feat);\n    dispatcher.Post(intent);\n    artifacts = 0;\n    await Task.Delay(SAMPLE_MS);\n}',
        'foreach (var kv in metrics.Snapshot()) {\n    if (!thresholds.TryGetValue(\n            kv.Key, out var thr)) {\n        continue;\n    }\n    if (kv.Value > thr.Warn &&\n        kv.Value <= thr.Crit) {\n        await alerting.WarnAsync(\n            kv.Key, kv.Value, thr.Warn);\n    } else if (kv.Value > thr.Crit) {\n        await alerting.CriticalAsync(\n            kv.Key, kv.Value, thr.Crit);\n        if (thr.AutoRemediate != null) {\n            await thr.AutoRemediate(\n                kv.Key, kv.Value);\n        }\n    }\n    await telemetry.EmitAsync(\n        kv.Key, kv.Value,\n        DateTime.UtcNow);\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        var snap = await db\n            .SnapshotAsync(cts.Token);\n        var hash = sha256.Hash(\n            snap.Serialize());\n        if (hash != expectedHash) {\n            log.Fatal(\n                $"db snapshot tamper: {hash:X}");\n            await sec.RaiseIncidentAsync(\n                IncidentType.DbTamper);\n            return;\n        }\n        expectedHash = hash;\n        snap.WriteTo(archivePath);\n        log.Info(\n            $"db snapshot ok lsn={snap.Lsn:X}");\n        await Task.Delay(\n            SNAPSHOT_INTERVAL_MS, cts.Token);\n    }\n});',
        'for (int pass = 0; pass < SCRUB_PASSES; pass++) {\n    for (ulong addr = region.Base;\n         addr < region.Base + region.Size;\n         addr += PAGE_SIZE) {\n        var page = mem.ReadPage(addr);\n        var hash = sha256.Hash(page);\n        if (!hashMap.TryGetValue(\n                addr, out var expected)) {\n            hashMap[addr] = hash;\n            continue;\n        }\n        if (hash != expected) {\n            log.Error(\n                $"mem tamper page {addr:X16}");\n            tamper.Add(addr);\n        }\n    }\n}\nif (tamper.Count > 0) {\n    await sec.RaiseIncidentAsync(\n        IncidentType.MemTamper);\n}',
        'foreach (var edge in graph\n    .Edges\n    .GroupBy(e => e.Type)\n    .OrderByDescending(g => g.Count())) {\n    var type  = edge.Key;\n    var count = edge.Count();\n    var avgW  = edge.Average(e => e.Weight);\n    log.Debug(\n        $"edge type {type}: n={count} avgW={avgW:F2}");\n    if (count > EDGE_TYPE_WARN_LIMIT) {\n        log.Warn(\n            $"edge type {type} count {count} — unusual");\n    }\n    stats.EdgesByType[type] = count;\n}',
        'while (net.IsConnected) {\n    if (!await net.SendHeartbeatAsync(\n            HB_TIMEOUT_MS, cts.Token)) {\n        hbMisses++;\n        log.Warn(\n            $"heartbeat miss #{hbMisses}");\n        if (hbMisses >= HB_DEAD_THRESHOLD) {\n            log.Error("peer dead — closing");\n            net.Close();\n            break;\n        }\n        continue;\n    }\n    hbMisses = 0;\n    var latency = net.LastRtt;\n    if (latency > LATENCY_WARN_MS) {\n        log.Warn(\n            $"rtt {latency}ms above threshold");\n    }\n    await Task.Delay(\n        HB_INTERVAL_MS, cts.Token);\n}',
        'Task.Run(async () => {\n    await foreach (var event2 in corp\n        .StreamAuditEventsAsync(cts.Token)) {\n        if (event2.Timestamp <\n            lastProcessed + MIN_SPACING) {\n            skipped++;\n            continue;\n        }\n        if (!event2.Signature.Verify(\n                corpKey, event2.Payload)) {\n            log.Error(\n                $"audit sig fail: {event2.Id}");\n            await sec.RaiseIncidentAsync(\n                IncidentType.AuditTamper);\n            continue;\n        }\n        await index.IndexAsync(event2);\n        lastProcessed = event2.Timestamp;\n        ingested++;\n    }\n    log.Info(\n        $"audit stream done: in={ingested} skip={skipped}");\n});',
        'for (int r = 0; r < MAX_RECONNECT; r++) {\n    try {\n        var conn = await db.ConnectAsync(\n            connectionString, cts.Token);\n        if (await conn.PingAsync()) {\n            log.Info(\n                $"db reconnected attempt {r}");\n            return conn;\n        }\n    } catch (DbException ex) {\n        log.Warn(\n            $"db connect #{r}: {ex.Message}");\n    }\n    var delay = Math.Min(\n        DB_BASE_MS * (1 << r), DB_MAX_MS);\n    await Task.Delay(delay, cts.Token);\n}\nthrow new DbConnectionException(\n    $"failed after {MAX_RECONNECT} attempts");',
        'foreach (var node in topology\n    .GetExpiredNodes(DateTime.UtcNow)) {\n    var probe = await net\n        .ReachabilityProbeAsync(\n            node.Endpoint,\n            PROBE_TIMEOUT_MS,\n            cts.Token);\n    if (probe.IsReachable) {\n        node.ValidTo =\n            DateTime.UtcNow + LEASE_DURATION;\n        topology.Renew(node);\n        log.Debug(\n            $"node {node.Id} lease renewed");\n    } else {\n        topology.Remove(node.Id);\n        graph.RemoveNode(node.Id);\n        log.Info(\n            $"node {node.Id} expired + pruned");\n    }\n}',
        'Task.Run(async () => {\n    var throttle = new TokenBucket(\n        RATE_PER_SEC, BURST);\n    while (!cts.IsCancellationRequested) {\n        var packet = await ingress\n            .DequeueAsync(cts.Token);\n        if (!throttle.TryConsume(1)) {\n            log.Warn(\n                $"rate limit drop: {packet.Src}");\n            metrics.Dropped++;\n            continue;\n        }\n        if (firewall.Deny(packet)) {\n            log.Info(\n                $"fw deny: {packet.Src} → {packet.Dst}");\n            metrics.Denied++;\n            continue;\n        }\n        await egress.EnqueueAsync(\n            packet, cts.Token);\n        metrics.Forwarded++;\n    }\n});',
        'for (int shard = 0; shard < NUM_SHARDS; shard++) {\n    var keys = await db.KeyRangeAsync(\n        shard * SHARD_SIZE,\n        (shard + 1) * SHARD_SIZE);\n    for (int i = 0; i < keys.Count; i++) {\n        var val = await db.GetAsync(keys[i]);\n        var h   = sha256.Hash(\n            Encoding.UTF8.GetBytes(\n                keys[i]) .Concat(val) .ToArray());\n        merkle.Set(shard, i, h);\n    }\n    var root = merkle.ComputeRoot(shard);\n    if (root != expectedRoots[shard]) {\n        log.Error(\n            $"merkle mismatch shard {shard}");\n        corrupt.Add(shard);\n    }\n}',
        'while (rebalancer.HasWork) {\n    var move = rebalancer.NextMove();\n    log.Info(\n        $"rebalance: {move.Key} {move.Src} → {move.Dst}");\n    try {\n        var data = await src.GetAsync(move.Key);\n        await dst.PutAsync(move.Key, data);\n        await src.DeleteAsync(move.Key);\n        rebalancer.Complete(move);\n        moved++;\n    } catch (Exception ex) {\n        log.Warn(\n            $"rebalance fail {move.Key}: {ex.Message}");\n        rebalancer.Retry(move);\n        failed++;\n        if (failed > MAX_REBALANCE_FAILS) {\n            throw new RebalanceException(failed);\n        }\n    }\n}',
        'foreach (var profile in bci\n    .GetOperatorProfiles()\n    .Where(p => p.KeyAge >\n        TimeSpan.FromDays(KEY_ROTATE_DAYS))) {\n    log.Warn(\n        $"bci key age {profile.KeyAge.Days}d: {profile.Uid}");\n    try {\n        var newKey = await kdf\n            .DeriveOperatorKeyAsync(\n                profile.Uid,\n                profile.NeuralSeed,\n                cts.Token);\n        await profile.RotateKeyAsync(\n            newKey, cts.Token);\n        audit.Record(\n            AuditEvent.KeyRotated,\n            profile.Uid);\n        rotated++;\n    } catch (Exception ex) {\n        log.Error(\n            $"key rotate fail {profile.Uid}: {ex.Message}");\n    }\n}',
        'Task.Run(async () => {\n    var circuit = new CircuitBreaker(\n        MAX_FAILURES, RESET_MS);\n    while (!cts.IsCancellationRequested) {\n        if (!circuit.IsAllowed) {\n            await Task.Delay(\n                circuit.RetryAfterMs, cts.Token);\n            continue;\n        }\n        try {\n            var result = await upstream\n                .CallAsync(request, cts.Token);\n            circuit.RecordSuccess();\n            return result;\n        } catch (Exception ex) {\n            circuit.RecordFailure();\n            log.Warn(\n                $"upstream fail #{circuit.FailCount}: {ex.Message}");\n            if (!circuit.IsAllowed) {\n                log.Error("circuit open — isolating");\n            }\n        }\n    }\n});',
        'for (int i = 0; i < revocationList.Count; i++) {\n    var entry = revocationList[i];\n    if (entry.Timestamp <\n        DateTime.UtcNow - MAX_CRL_AGE) {\n        log.Warn(\n            $"crl entry {entry.Serial:X} stale — remove");\n        revocationList.RemoveAt(i--);\n        pruned++;\n        continue;\n    }\n    if (certStore.Contains(entry.Thumbprint)) {\n        certStore.Revoke(entry.Thumbprint);\n        log.Info(\n            $"revoked: {entry.Subject}");\n        revoked2++;\n    }\n}\nlog.Info(\n    $"crl prune: pruned={pruned} revoked={revoked2}");',
        'while (incoming.HasData) {\n    var req = await incoming\n        .ReadRequestAsync(cts.Token);\n    if (!authz.Authorize(\n            req.Token, req.Resource)) {\n        await outgoing.WriteAsync(\n            Response.Deny(req.Id));\n        audit.Deny(\n            req.Token.Subject, req.Resource);\n        denied++;\n        continue;\n    }\n    try {\n        var resp = await handler\n            .ProcessAsync(req, cts.Token);\n        await outgoing.WriteAsync(resp);\n        audit.Allow(\n            req.Token.Subject, req.Resource);\n        ok++;\n    } catch (Exception ex) {\n        await outgoing.WriteAsync(\n            Response.Error(req.Id, ex.Message));\n        errors++;\n    }\n}',
        'foreach (var page in journal\n    .GetDirtyPages()\n    .OrderBy(p => p.Lsn)) {\n    await db.WritePageAsync(page);\n    journal.MarkFlushed(page.Lsn);\n    flushed++;\n    if (flushed % FSYNC_EVERY == 0) {\n        await db.FsyncAsync();\n        log.Debug(\n            $"fsync after {flushed} pages");\n    }\n    if (journal.UnflushedCount == 0) {\n        log.Info(\n            $"journal fully flushed lsn={page.Lsn:X}");\n        break;\n    }\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        var frame = await bci\n            .RecvFrameAsync(cts.Token);\n        if (frame.Seq != expected) {\n            log.Warn(\n                $"bci seq gap: {expected}→{frame.Seq}");\n            gaps += frame.Seq - expected;\n            expected = frame.Seq + 1;\n        } else {\n            expected++;\n        }\n        var decrypted = aes.Decrypt(\n            frame.Payload, frame.Iv, sessionKey);\n        var sample = BciSample\n            .Deserialize(decrypted);\n        await pipeline.FeedAsync(\n            sample, cts.Token);\n        metrics.Frames++;\n    }\n});',
        'for (int t = 0; t < threads.Count; t++) {\n    var thread = threads[t];\n    if (!thread.IsAlive) {\n        log.Warn(\n            $"thread {t} dead — restart");\n        threads[t] = threadFactory\n            .Create(threadConfig[t]);\n        threads[t].Start();\n        restarted++;\n        continue;\n    }\n    if (thread.BlockedMs > BLOCKED_WARN_MS) {\n        log.Warn(\n            $"thread {t} blocked {thread.BlockedMs}ms");\n        if (thread.BlockedMs > DEADLOCK_MS) {\n            log.Fatal(\n                $"thread {t} deadlock — kill");\n            thread.Kill();\n            threads[t] = threadFactory\n                .Create(threadConfig[t]);\n            threads[t].Start();\n        }\n    }\n}',
        'foreach (var entry in arp\n    .GetTable()\n    .Where(e => e.Flags\n        .HasFlag(ArpFlags.Dynamic))) {\n    if (DateTime.UtcNow - entry.LastSeen >\n        ARP_STALE_TIMEOUT) {\n        arp.Remove(entry.Ip);\n        log.Debug(\n            $"arp stale: {entry.Ip} → {entry.Mac}");\n        stale++;\n        continue;\n    }\n    if (poisonDetector.IsSuspect(\n            entry.Ip, entry.Mac)) {\n        log.Warn(\n            $"arp poison suspect: {entry.Ip}");\n        await sec.RaiseIncidentAsync(\n            IncidentType.ArpPoison);\n    }\n}',
        'Task.Run(async () => {\n    var detector = new AnomalyDetector(\n        WINDOW_SIZE, SIGMA_THRESHOLD);\n    await foreach (var metric in telemetry\n        .StreamAsync(cts.Token)) {\n        detector.Feed(metric.Key, metric.Value);\n        if (detector.IsAnomaly(\n                metric.Key, metric.Value)) {\n            var score = detector.Score(\n                metric.Key, metric.Value);\n            log.Warn(\n                $"anomaly {metric.Key}={metric.Value} score={score:F2}");\n            await siem.IngestAsync(\n                new AnomalyEvent(\n                    metric.Key,\n                    metric.Value,\n                    score,\n                    DateTime.UtcNow));\n        }\n    }\n});',
        'for (int b = 0; b < backup.BlockCount; b++) {\n    var blk    = backup.ReadBlock(b);\n    var actual = Crc32c.Compute(blk);\n    if (actual != backup.Checksum(b)) {\n        log.Error(\n            $"backup block {b} crc fail: {actual:X}");\n        bad.Add(b);\n        if (bad.Count > MAX_BAD_BLOCKS) {\n            throw new BackupCorruptException(\n                bad.Count);\n        }\n        continue;\n    }\n    await restore.WriteBlockAsync(b, blk);\n    if (b % VERIFY_SAMPLE == 0) {\n        var readback = await restore\n            .ReadBlockAsync(b);\n        if (!readback.SequenceEqual(blk)) {\n            log.Error(\n                $"restore verify fail blk {b}");\n        }\n    }\n}',
        'while (scheduler.HasReadyJobs) {\n    var job = scheduler.Dequeue();\n    if (quota.IsExceeded(job.OwnerId)) {\n        log.Warn(\n            $"quota exceeded: {job.OwnerId}");\n        scheduler.Defer(\n            job, QUOTA_RETRY_MS);\n        continue;\n    }\n    var worker = pool.TryAcquire();\n    if (worker == null) {\n        scheduler.Defer(\n            job, POOL_FULL_RETRY_MS);\n        poolFull++;\n        continue;\n    }\n    quota.Consume(job.OwnerId, 1);\n    await worker.RunAsync(\n        job, cts.Token);\n    pool.Release(worker);\n    completed++;\n}',
        'foreach (var identity in corp\n    .GetExpiredIdentities(\n        DateTime.UtcNow)) {\n    if (identity.HasActiveSession) {\n        await session\n            .InvalidateAsync(identity.Id);\n        log.Info(\n            $"session revoked: {identity.Id}");\n    }\n    corp.Expire(identity.Id);\n    audit.Record(\n        AuditEvent.IdentityExpired,\n        identity.Id);\n    expired.Add(identity.Id);\n    if (identity.HasBciKey) {\n        await bci.RevokeKeyAsync(\n            identity.BciKeyId);\n        log.Info(\n            $"bci key revoked: {identity.BciKeyId}");\n    }\n}',
        'Task.Run(async () => {\n    var hmacKey = await vault\n        .GetHmacKeyAsync(KEY_ID, cts.Token);\n    while (!cts.IsCancellationRequested) {\n        var msg = await queue\n            .DequeueAsync(cts.Token);\n        var tag = hmac.Compute(\n            hmacKey, msg.Payload);\n        if (!tag.SequenceEqual(msg.Tag)) {\n            log.Error(\n                $"hmac mismatch msg {msg.Id}");\n            await sec.RaiseIncidentAsync(\n                IncidentType.HmacFail);\n            continue;\n        }\n        await processor\n            .HandleAsync(msg, cts.Token);\n        metrics.Verified++;\n    }\n});',
        'for (int i = 0; i < graph.NodeCount; i++) {\n    var node = graph.GetNodeAt(i);\n    var neighbors = graph\n        .GetNeighbors(node.Id)\n        .ToList();\n    if (neighbors.Count == 0) {\n        if (node.IsPrunable) {\n            graph.RemoveNode(node.Id);\n            pruned++;\n        }\n        continue;\n    }\n    node.Degree = neighbors.Count;\n    node.ClusterCoeff =\n        graphMath.ClusterCoefficient(\n            node.Id, neighbors);\n    if (node.ClusterCoeff < MIN_CLUSTER) {\n        log.Debug(\n            $"node {node.Id} low cluster {node.ClusterCoeff:F2}");\n    }\n    db.UpdateNode(node);\n}',
        'while (hkb.IsMonitoring) {\n    var stroke = await hkb\n        .AwaitStrokeAsync(cts.Token);\n    log.Debug(\n        $"hkb stroke: force={stroke.Force:F1}N duration={stroke.DurationMs}ms");\n    if (stroke.Force > HKB_MAX_FORCE) {\n        log.Warn(\n            $"hkb force {stroke.Force:F1}N exceeds {HKB_MAX_FORCE}N");\n        safety.FlagOverpressure();\n    }\n    absorb = hkb.CalcAbsorption(\n        stroke.Force, stroke.DurationMs);\n    perceived = stroke.Force - absorb;\n    log.Debug(\n        $"hkb absorb={absorb:F1}N perceived={perceived:F1}N");\n    metrics.HkbStrokes++;\n    metrics.TotalAbsorbed += absorb;\n}',
        'foreach (var key in keyStore\n    .GetAll()\n    .Where(k => k.Algorithm == KeyAlg.Ed25519\n             && k.NotAfter <\n                DateTime.UtcNow\n                    .AddDays(RENEW_BEFORE_DAYS))) {\n    log.Info(\n        $"key {key.Id} near expiry — rotating");\n    var newKey = Ed25519.GenerateKey();\n    var newId  = keyStore.Store(newKey,\n        KeyAlg.Ed25519, KEY_VALIDITY_DAYS);\n    foreach (var svc in services\n        .GetUsingKey(key.Id)) {\n        await svc.UpdateKeyAsync(\n            key.Id, newId, cts.Token);\n    }\n    keyStore.Retire(key.Id);\n    audit.Record(\n        AuditEvent.KeyRotated, key.Id);\n}',
        'Task.Run(async () => {\n    await using var stream =\n        await corp.OpenMirrorStreamAsync(\n            mirrorId, cts.Token);\n    while (!cts.IsCancellationRequested) {\n        var delta = await stream\n            .ReadDeltaAsync(cts.Token);\n        if (delta.IsEmpty) continue;\n        lag = DateTime.UtcNow -\n            delta.PrimaryTimestamp;\n        if (lag > LAG_WARN_THRESHOLD) {\n            log.Warn(\n                $"mirror lag {lag.TotalSeconds:F1}s");\n        }\n        await localDb\n            .ApplyDeltaAsync(delta);\n        synced += delta.OpCount;\n    }\n    log.Info(\n        $"mirror stream end: synced={synced}");\n});',
        'for (int i = 0; i < events.Count; i++) {\n    if (!correlator.TryCorrelate(\n            events[i], window,\n            out var group)) {\n        uncorrelated++;\n        continue;\n    }\n    if (group.Score > ALERT_SCORE) {\n        var alert = new CorrelationAlert(\n            group.RuleId,\n            group.Events,\n            group.Score);\n        await siem\n            .IngestAlertAsync(\n                alert, cts.Token);\n        alerts++;\n    }\n    processed++;\n}\nlog.Info(\n    $"correlate: proc={processed} alert={alerts} uncorr={uncorrelated}");',
        'while (lsm.HasPendingCompaction) {\n    var level = lsm.NextCompactionLevel();\n    var inputs = lsm.GetCompactionInputs(level);\n    var output = lsm.NewOutputFile(level + 1);\n    var merger = new SstMerger(\n        inputs, output, lsm.GcHorizon);\n    while (merger.MoveNext()) {\n        output.Write(merger.Current);\n        written++;\n        if (written % 10_000 == 0) {\n            log.Debug(\n                $"compact lv{level}: {written} kv");\n        }\n    }\n    lsm.CompleteCompaction(\n        level, inputs, output);\n    log.Info(\n        $"compact lv{level}: {written} kv done");\n}',
        'foreach (var operator3 in glmz\n    .GetActiveOperators(district)\n    .Where(o => enforcement\n        .IsOnWatchlist(o.Id))) {\n    var events2 = await audit\n        .GetRecentAsync(\n            operator3.Id,\n            LOOKBACK_HOURS,\n            cts.Token);\n    var score  = riskScorer\n        .Score(operator3, events2);\n    if (score > RISK_ESCALATE) {\n        log.Warn(\n            $"operator {operator3.Id:X} risk={score:F2}");\n        await enforcement\n            .EscalateAsync(\n                operator3.Id, score);\n    } else {\n        enforcement\n            .RecordCheck(operator3.Id);\n    }\n}',
        'Task.Run(async () => {\n    var trie = new PatriciaRadixTrie();\n    while (!cts.IsCancellationRequested) {\n        var update = await routing\n            .DequeueUpdateAsync(cts.Token);\n        switch (update.Type) {\n            case RouteUpdate.Advertise:\n                trie.Insert(\n                    update.Prefix,\n                    update.NextHop);\n                log.Debug(\n                    $"route add {update.Prefix}");\n                break;\n            case RouteUpdate.Withdraw:\n                trie.Remove(update.Prefix);\n                log.Debug(\n                    $"route del {update.Prefix}");\n                break;\n        }\n        fib.Rebuild(trie);\n        metrics.RouteCount =\n            trie.Count;\n    }\n});',
        'for (int s = 0; s < samplers.Count; s++) {\n    var sample = await samplers[s]\n        .SampleAsync(cts.Token);\n    if (sample == null) continue;\n    var normalized = normalizer\n        .Normalize(sample, baseline[s]);\n    var anomaly    = detector\n        .Check(s, normalized);\n    if (anomaly.Detected) {\n        log.Warn(\n            $"sampler {s} anomaly: {anomaly.Score:F3} σ={anomaly.Sigma:F1}");\n        if (anomaly.Sigma > CRITICAL_SIGMA) {\n            await alerting.CriticalAsync(\n                s, anomaly);\n        } else {\n            await alerting.WarnAsync(\n                s, anomaly);\n        }\n    }\n    metrics.Samples[s] = sample;\n}',
        'while (!shutdown.IsCancellationRequested) {\n    var job = await queue\n        .DequeueAsync(DEQUEUE_TIMEOUT_MS,\n                      cts.Token);\n    if (job == null) {\n        if (++idleTicks > MAX_IDLE_TICKS) {\n            log.Debug("worker idle — suspend");\n            await Task.Delay(\n                IDLE_SUSPEND_MS, cts.Token);\n            idleTicks = 0;\n        }\n        continue;\n    }\n    idleTicks = 0;\n    var sw = Stopwatch.StartNew();\n    try {\n        await job.ExecuteAsync(cts.Token);\n        metrics.Done++;\n        log.Debug(\n            $"job {job.Id} done {sw.ElapsedMs}ms");\n    } catch (Exception ex) {\n        metrics.Failed++;\n        log.Error(\n            $"job {job.Id} error: {ex.Message}");\n        await queue.RetryAsync(job);\n    }\n}',
    ];

    var tickTimer = null;

    function rand(a, b) { return Math.floor(Math.random() * (b - a + 1)) + a; }
    function pick(arr)  { return arr[rand(0, arr.length - 1)]; }
    function getHost()  { return document.querySelector('.console-bg-host'); }

    // Returns the bottom edge of .board-grid as a % of viewport height, +2% buffer.
    // Spawn positions use this as their minimum top value so nothing overlaps tiles.
    // randTop: unrestricted — effects can spawn anywhere including over tiles
    function randTop(lo, hi) { return rand(lo, hi); }

    // Try 4 random positions, pick the one with the least overlap against existing large overlays.
    // estW/estH are pixel estimates of the element being spawned.
    function bestPos(host, estW, estH, xMin, xMax, yMin, yMax) {
        var hw = window.innerWidth, hh = window.innerHeight;
        var sel = host.querySelectorAll('.cbg-win,.cbg-err-popup,.cbg-warn-popup,.cbg-memo');
        var rects = [];
        for (var i = 0; i < sel.length; i++) rects.push(sel[i].getBoundingClientRect());
        var bx = rand(xMin, xMax), by = rand(yMin, yMax), bov = Infinity;
        for (var t = 0; t < 4; t++) {
            var cx = rand(xMin, xMax), cy = rand(yMin, yMax);
            var px = (cx / 100) * hw, py = (cy / 100) * hh;
            var ov = 0;
            for (var r = 0; r < rects.length; r++) {
                var rc = rects[r];
                var ox = Math.max(0, Math.min(px + estW, rc.right)  - Math.max(px, rc.left));
                var oy = Math.max(0, Math.min(py + estH, rc.bottom) - Math.max(py, rc.top));
                ov += ox * oy;
            }
            if (ov < bov) { bov = ov; bx = cx; by = cy; }
        }
        return [bx, by];
    }

    // ── Terminal windows ────────────────────────────────────────────────────

    function spawnWindow(extraDelay, posX, posY, extraClass) {
        setTimeout(function () {
            var host = getHost();
            if (!host) return;

            // ~20% chance the window lingers, waiting for input
            var waiting = Math.random() < 0.20;

            var win = document.createElement('div');
            var colorVar = pick(['', '', 'cbg-win--blue', 'cbg-win--amber']);
            win.className = 'cbg-win' + (colorVar ? ' ' + colorVar : '') + (extraClass ? ' ' + extraClass : '');
            var wp = posX !== undefined ? [posX, posY] : bestPos(host, 228, 140, -8, 88, 4, 76);
            win.style.left = wp[0] + '%';
            win.style.top  = wp[1] + '%';

            var titleEl = document.createElement('div');
            titleEl.className = 'cbg-title';
            titleEl.textContent = pick(TITLES);
            win.appendChild(titleEl);

            var body = document.createElement('div');
            body.className = 'cbg-body';
            win.appendChild(body);

            host.appendChild(win);

            var lineCount = rand(2, 6);
            var lines = [];
            for (var i = 0; i < lineCount; i++) { lines.push(pick(LINES)); }

            var success = !waiting && Math.random() > 0.32;
            var result  = pick(success ? OK_RESULTS : ERR_RESULTS);
            var resCls  = success ? 'cbg-ok' : 'cbg-err';

            var idx = 0;
            var lineTimer = setInterval(function () {
                if (idx < lines.length) {
                    var ln = document.createElement('div');
                    ln.className = 'cbg-line';
                    ln.textContent = lines[idx];
                    body.appendChild(ln);
                    idx++;
                } else {
                    clearInterval(lineTimer);
                    setTimeout(function () {
                        if (waiting) {
                            // Show a prompt line with blinking cursor — no result, lingers
                            var prompt = document.createElement('div');
                            prompt.className = 'cbg-line cbg-prompt';
                            prompt.innerHTML = '> <span class="cbg-cursor">&#9608;</span>';
                            body.appendChild(prompt);
                            // Long random TTL: 6–18 seconds
                            setTimeout(function () {
                                win.classList.add('cbg-win--out');
                                setTimeout(function () {
                                    if (win.parentNode) win.parentNode.removeChild(win);
                                }, 160);
                            }, rand(6000, 18000));
                        } else {
                            var res = document.createElement('div');
                            res.className = 'cbg-line ' + resCls;
                            res.textContent = result;
                            body.appendChild(res);

                            setTimeout(function () {
                                win.classList.add('cbg-win--out');
                                setTimeout(function () {
                                    if (win.parentNode) win.parentNode.removeChild(win);
                                }, 160);
                            }, rand(700, 2500));
                        }
                    }, 130);
                }
            }, rand(4, 12));
        }, extraDelay || 0);
    }

    function spawnCascade() {
        var n = rand(3, 6);
        var x = rand(-5, 75), y = randTop(3, 85);
        var stepX = rand(5, 12), stepY = rand(4, 9);
        for (var i = 0; i < n; i++) {
            spawnWindow(i * rand(65, 210), x + i * stepX, y + i * stepY, 'cbg-cascade');
        }
    }

    // ── Fatal error popup ───────────────────────────────────────────────────

    function spawnError(posX, posY) {
        var host = getHost();
        if (!host) return;

        var popup = document.createElement('div');
        popup.className = 'cbg-err-popup';
        var ep = posX !== undefined ? [posX, posY] : bestPos(host, 310, 90, -5, 80, 5, 88);
        popup.style.left = ep[0] + '%';
        popup.style.top  = ep[1] + '%';

        // Layout: [red icon] | [title \n message \n ... \n OK btn]
        var icon = document.createElement('div');
        icon.className = 'cbg-err-popup-icon';
        icon.textContent = '⬛';   // replaced visually via CSS — just a block placeholder
        popup.appendChild(icon);

        var content = document.createElement('div');
        content.className = 'cbg-err-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-err-popup-title';
        titleEl.textContent = pick(FATAL_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cbg-err-popup-msg';
        msgEl.textContent = pick(FATAL_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);

        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cbg-win--out');
            setTimeout(function () {
                if (popup.parentNode) popup.parentNode.removeChild(popup);
            }, 160);
        }, rand(1800, 4000));
    }

    // ── Warning popup ───────────────────────────────────────────────────────

    function spawnWarning(posX, posY) {
        var host = getHost();
        if (!host) return;

        var popup = document.createElement('div');
        popup.className = 'cbg-warn-popup';
        var wp2 = posX !== undefined ? [posX, posY] : bestPos(host, 290, 90, -5, 82, 5, 88);
        popup.style.left = wp2[0] + '%';
        popup.style.top  = wp2[1] + '%';

        var content = document.createElement('div');
        content.className = 'cbg-warn-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-warn-popup-title';
        titleEl.textContent = pick(WARN_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cbg-warn-popup-msg';
        msgEl.textContent = pick(WARN_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);
        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cbg-win--out');
            setTimeout(function () {
                if (popup.parentNode) popup.parentNode.removeChild(popup);
            }, 160);
        }, rand(2500, 5500));
    }


    // ── Floating code fragments ─────────────────────────────────────────────

    function spawnFrag() {
        var host = getHost();
        if (!host) return;

        var el = document.createElement('div');
        el.className = 'cbg-frag';
        el.style.left = rand(-4, 94) + '%';
        el.style.top  = randTop(2, 94) + '%';
        host.appendChild(el);

        var text = pick(FRAGS);
        var idx  = 0;
        var cur  = '';

        var typeTimer = setInterval(function () {
            if (idx >= text.length) {
                clearInterval(typeTimer);
                setTimeout(function () {
                    el.classList.add('cbg-frag--out');
                    setTimeout(function () {
                        if (el.parentNode) el.parentNode.removeChild(el);
                    }, 360);
                }, rand(18, 75));
                return;
            }
            cur += text[idx++];
            el.textContent = cur;
        }, rand(3, 7));
    }

    // ── Geometry schematic window ────────────────────────────────────────────

    var PHI = (1 + Math.sqrt(5)) / 2;
    var GEO_SHAPES = {
        tetra:  { label:'tetrahedron',  verts:[[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1]], edges:[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]] },
        cube:   { label:'hexahedron',   verts:[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]], edges:[[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7]] },
        octa:   { label:'octahedron',   verts:[[0,1,0],[0,-1,0],[1,0,0],[-1,0,0],[0,0,1],[0,0,-1]], edges:[[0,2],[0,3],[0,4],[0,5],[1,2],[1,3],[1,4],[1,5],[2,4],[4,3],[3,5],[5,2]] },
        icosa:    { label:'icosahedron',     verts:[[0,1,PHI],[0,-1,PHI],[0,1,-PHI],[0,-1,-PHI],[1,PHI,0],[-1,PHI,0],[1,-PHI,0],[-1,-PHI,0],[PHI,0,1],[-PHI,0,1],[PHI,0,-1],[-PHI,0,-1]], edges:[[0,1],[0,4],[0,5],[0,8],[0,9],[1,6],[1,7],[1,8],[1,9],[2,3],[2,4],[2,5],[2,10],[2,11],[3,6],[3,7],[3,10],[3,11],[4,5],[4,8],[4,10],[5,9],[5,11],[6,7],[6,8],[6,10],[7,9],[7,11],[8,10],[9,11]] },
        prism:    { label:'triangular prism',  verts:[[0,1,1],[0.866,-0.5,1],[-0.866,-0.5,1],[0,1,-1],[0.866,-0.5,-1],[-0.866,-0.5,-1]], edges:[[0,1],[1,2],[2,0],[3,4],[4,5],[5,3],[0,3],[1,4],[2,5]] },
        stella:   { label:'stella octangula',  verts:[[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1],[-1,-1,-1],[1,1,-1],[1,-1,1],[-1,1,1]], edges:[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3],[4,5],[4,6],[4,7],[5,6],[5,7],[6,7]] },
        cubocta:  { label:'cuboctahedron',     verts:[[1,1,0],[-1,1,0],[1,-1,0],[-1,-1,0],[1,0,1],[-1,0,1],[1,0,-1],[-1,0,-1],[0,1,1],[0,-1,1],[0,1,-1],[0,-1,-1]], edges:[[0,4],[0,6],[0,8],[0,10],[1,5],[1,7],[1,8],[1,10],[2,4],[2,6],[2,9],[2,11],[3,5],[3,7],[3,9],[3,11],[4,8],[4,9],[5,8],[5,9],[6,10],[6,11],[7,10],[7,11]] },
        antiprism:{ label:'square antiprism',  verts:[[1,1,0],[0,1,1],[-1,1,0],[0,1,-1],[0.707,-1,0.707],[-0.707,-1,0.707],[-0.707,-1,-0.707],[0.707,-1,-0.707]], edges:[[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[0,7],[1,4],[1,5],[2,5],[2,6],[3,6],[3,7]] },
        pyramid:  { label:'square pyramid',    verts:[[0,1.2,0],[1,-0.5,1],[-1,-0.5,1],[-1,-0.5,-1],[1,-0.5,-1]], edges:[[0,1],[0,2],[0,3],[0,4],[1,2],[2,3],[3,4],[4,1]] },
        pentaprism:{ label:'pentagonal prism', verts:[[1,1,0],[0.309,1,0.951],[-0.809,1,0.588],[-0.809,1,-0.588],[0.309,1,-0.951],[1,-1,0],[0.309,-1,0.951],[-0.809,-1,0.588],[-0.809,-1,-0.588],[0.309,-1,-0.951]], edges:[[0,1],[1,2],[2,3],[3,4],[4,0],[5,6],[6,7],[7,8],[8,9],[9,5],[0,5],[1,6],[2,7],[3,8],[4,9]] },
        dodeca:   { label:'dodecahedron',      verts:[[1,1,1],[1,1,-1],[1,-1,1],[1,-1,-1],[-1,1,1],[-1,1,-1],[-1,-1,1],[-1,-1,-1],[0,0.618,1.618],[0,0.618,-1.618],[0,-0.618,1.618],[0,-0.618,-1.618],[0.618,1.618,0],[0.618,-1.618,0],[-0.618,1.618,0],[-0.618,-1.618,0],[1.618,0,0.618],[1.618,0,-0.618],[-1.618,0,0.618],[-1.618,0,-0.618]], edges:[[0,8],[0,12],[0,16],[1,9],[1,12],[1,17],[2,10],[2,13],[2,16],[3,11],[3,13],[3,17],[4,8],[4,14],[4,18],[5,9],[5,14],[5,19],[6,10],[6,15],[6,18],[7,11],[7,15],[7,19],[8,10],[9,11],[12,14],[13,15],[16,17],[18,19]] }
    };
    var GEO_KEYS = ['tetra','cube','octa','icosa','flower','metatron','prism','stella','cubocta','antiprism','vesica','spiral','lissajous','star5','torus','helix','dodeca','pyramid','pentaprism','rose','cardioid','asteroid','epicycloid','web'];

    // ── Geo-window element report lines ────────────────────────────────────
    var GEO_REPORT_LINES = [
        'Atomic Mass:   [REDACTED]',
        'Isotope Δ:     +0.00441 amu',
        'Valence:       4f¹⁴5d¹⁰6p²',
        'Melting Pt:    3,817°K ± 0.3',
        'Boiling Pt:    [UNSTABLE]',
        'Half-life:     τ = 4.41×10⁻⁷s',
        'Nuclear Spin:  7/2',
        'Isomer State:  m2 confirmed',
        'Bind Energy:   8.812 MeV/nuc',
        'Cross Sect:    σ = 3.301 barn',
        'Decay Chain:   α→β⁻→γ cascade',
        'Oxidation:     +3, +5, +7',
        'Crystal Sys:   Orthorhombic',
        'Density:       19.77 g/cm³',
        'Resistivity:   2.4×10⁻⁸ Ω·m',
        'Band Gap:      0.441 eV',
        'Fermi Level:   4.92 eV',
        'Curie Temp:    1,187 K',
        'Neel Temp:     [classified]',
        'X-Ray Kα:      88.12 keV',
        'Emittance ε:   0.031',
        'Work Fnct φ:   4.41 eV',
        'Plasma ωₚ:     8.8×10¹⁵ Hz',
        'Refract n:     3.301+0.441i',
        'Reflectance:   R = 0.77',
        'Thermal K:     44.1 W/(m·K)',
        'Heat Cp:       28.12 J/(mol·K)',
        'Compressib:    3.3×10⁻¹¹ Pa⁻¹',
        'Synth Method:  Heavy-ion fusion',
        'Discovery:     GLMZ Deep Lab',
        'IUPAC Name:    Unennilium (Φ)',
        'Alt. Name:     [REDACTED]',
        'Registry:      Ψ-441-GLMZ-Φ',
        'Status:        RESTRICTED',
        'Clearance:     CORP EYES ONLY',
        'Sample Mass:   0.0000441 μg',
        'Stability:     [CLASSIFIED]',
        'Yield:         3.301×10⁻¹⁴ %',
        'Hazard:        EXTREME',
        'Manifold:      R⁴ compact',
        'Curvature κ:   +0.00441',
        'Euler χ:       2 (sphere)',
        'Genus:         0 orientable',
        'Betti b₀:      1',
        'Symm Group:    Iₕ (order 120)',
        'Dihedral:      A₅ × Z₂',
        'Vertex deg:    3-regular',
        'Face type:     pentagonal',
        'Dual solid:    icosahedron',
        'Conway:        D(I) notation',
        'Wythoff:       3|2 5',
        'Schläfli:      {5,3}',
        'Vol/Area:      V/A = 0.332',
        'Circum-R:      1.401 norm',
        'Inrad-r:       1.113 norm',
        'Midrad ρ:      1.309 norm',
        'Petrie Poly:   decagon',
        'Dual Verts:    12',
        'Coord Ring:    ℝ[x,y,z]/I₅',
        'Packing η:     0.9069 max',
        'Lattice:       FCC equiv',
        'Point Group:   Oh (48 ops)',
        'Space Group:   Fm3̄m',
        'Fourier k:     2π/a = 1.772',
        'Mode ω₀:       [CLASSIFIED]',
        'Signal SNR:    38.8 dB',
        'Phase δ:       0.441 rad',
        'Resonance:     4,410 Hz',
        'Impedance:     441 + 88i Ω',
        // 100 new entries
        'Planck E:      6.626×10⁻³⁴ J·s',
        'Boltzmann k:   1.381×10⁻²³ J/K',
        'Avogadro N:    6.022×10²³ mol⁻¹',
        'Fine-struct α: 7.297×10⁻³',
        'Bohr Radius:   5.292×10⁻¹¹ m',
        'Rydberg Ry:    13.606 eV',
        'Magneton μ_B:  9.274×10⁻²⁴ J/T',
        'Gyromagn γ:    2.675×10⁸ rad/(s·T)',
        'Larmor ω:      4.41×10⁸ rad/s',
        'Spin-orbit ξ:  0.441 meV',
        'Zeeman Δ:      +0.088 meV/T',
        'Knight Shift:  0.0441%',
        'Hyperfine A:   441 MHz',
        'g-factor:      2.00441',
        'T₁ relax:      4.41 ms',
        'T₂ relax:      0.88 ms',
        'T₂* decay:     0.441 ms',
        'Echo TE:       8.812 ms',
        'Flip angle:    88.1°',
        'B₀ field:      3.301 T',
        'SAR limit:     4.0 W/kg',
        'Freq offset:   +441 Hz',
        'Gradient:      44.1 mT/m',
        'FOV:           441 mm²',
        'Resolution:    0.441 mm/px',
        'SNR acq:       44.1 dB',
        'CNR tissue:    12.4 dB',
        'k-space dc:    0.881 norm',
        'Phase encode:  128 lines',
        'Readout BW:    441 Hz/px',
        'TE/TR ratio:   0.0441',
        'Spectral ν:    4,410.0 cm⁻¹',
        'Raman shift:   441 cm⁻¹',
        'FWHM line:     0.88 cm⁻¹',
        'Absorbance A:  0.441 AU',
        'Transmit T:    36.2%',
        'Extinction ε:  4,410 L/(mol·cm)',
        'Beer-Lambert:  c = A/(ε·l)',
        'Fluoresc λem:  488 nm',
        'Stokes shift:  44 nm',
        'QY φ:          0.441',
        'Lifetime τ:    4.41 ns',
        'FRET eff E:    0.88',
        'FRET dist R:   4.41 nm',
        'Förster R₀:    5.88 nm',
        'Coord #:       8 (cubic)',
        'Bond length:   2.88 Å',
        'Bond angle:    109.44°',
        'Dihedral ψ:    44.1°',
        'Ramachand:     β-sheet region',
        'B-factor:      8.12 Å²',
        'RMSD:          0.441 Å',
        'Radius Gyrat:  4.41 nm',
        'Mol Weight:    44,120 Da',
        'pI:            4.41',
        'Δ G fold:      -44.1 kJ/mol',
        'Tm melt:       441 K',
        'Kd bind:       4.41×10⁻⁹ M',
        'kon:           4.41×10⁵ M⁻¹s⁻¹',
        'koff:          1.94×10⁻³ s⁻¹',
        'ΔH bind:       -88.1 kJ/mol',
        'ΔS bind:       -0.144 kJ/(mol·K)',
        'IC₅₀:         4.41 nM',
        'EC₅₀:         88.1 nM',
        'Hill n:        1.441',
        'Coord X:       [REDACTED] °N',
        'Coord Y:       [REDACTED] °E',
        'Alt Z:         -44.1 m GLMZ datum',
        'Bearing:       088.1°',
        'Range:         4.41 km',
        'Grid ref:      GLMZ-4412-7788',
        'Sector:        D-7 / Node-441',
        'Mag decl:      +4.4°',
        'Grid north:    0.88° west',
        'MGRS:          [REDACTED]',
        'Signal E-field: 4.41 kV/m',
        'Signal H-field: 0.441 A/m',
        'Power density: 8.8 mW/cm²',
        'Attenuation:   44.1 dB/km',
        'Gain dBi:      8.12 dBi',
        'Beamwidth:     4.41°',
        'Polarization:  [REDACTED]',
        'Doppler Δf:    +441 Hz',
        'Phase noise:   -88 dBc/Hz',
        'Spur level:    -44.1 dBc',
        'IP3:           +8.8 dBm',
        'NF:            4.41 dB',
        'IQ imbalance:  0.088°',
        'ADC bits:      [CLASSIFIED]',
        'Sample rate:   44.1 ksps',
        'Oversampling:  8×',
        'ENOB:          8.12 bits',
        'SFDR:          88.1 dBc',
        'THD:           -44.1 dB',
        'Enc Key ID:    0xBE0441',
        'Cipher Suite:  [CLASSIFIED]',
        'Key Entropy:   256 bits',
        'Nonce seq:     0x3AF7_441',
        'AEAD tag:      [REDACTED]',
    ];

    function spawnGeoWindow() {
        var host = getHost();
        if (!host) return;
        var key = pick(GEO_KEYS);
        var shape = GEO_SHAPES[key];

        var win = document.createElement('div');
        win.className = 'cbg-win cbg-geo-win';
        var gp = bestPos(host, 240, 130, -5, 80, 5, 90);
        win.style.left = gp[0] + '%';
        win.style.top  = gp[1] + '%';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-title';
        titleEl.textContent = 'schematic/' + (shape ? shape.label : key);
        win.appendChild(titleEl);

        var S = 110;
        var bodyEl = document.createElement('div');
        bodyEl.className = 'cbg-geo-body';
        var cv = document.createElement('canvas');
        cv.className = 'cbg-geo-canvas';
        cv.width = S; cv.height = S;
        bodyEl.appendChild(cv);
        win.appendChild(bodyEl);
        host.appendChild(win);

        var ctx2 = cv.getContext('2d');
        var angle = Math.random() * Math.PI * 2;
        var rafId = null;

        function proj3(v, rx, ry) {
            var x = v[0], y = v[1], z = v[2];
            var cry = Math.cos(ry), sry = Math.sin(ry);
            var x1 = x * cry + z * sry, z1 = -x * sry + z * cry;
            var crx = Math.cos(rx), srx = Math.sin(rx);
            var y1 = y * crx - z1 * srx, z2 = y * srx + z1 * crx;
            var d = 6 / (6 + z2 * 0.5);
            return [x1 * d * 32, y1 * d * 32];
        }

        function drawWire(sh, rx, ry) {
            var pts = sh.verts.map(function(v){ return proj3(v, rx, ry); });
            ctx2.strokeStyle = 'rgba(70,210,170,0.55)'; ctx2.lineWidth = 0.7;
            sh.edges.forEach(function(e){ ctx2.beginPath(); ctx2.moveTo(pts[e[0]][0],pts[e[0]][1]); ctx2.lineTo(pts[e[1]][0],pts[e[1]][1]); ctx2.stroke(); });
            ctx2.fillStyle = 'rgba(70,210,170,0.85)';
            pts.forEach(function(p){ ctx2.beginPath(); ctx2.arc(p[0],p[1],1.2,0,Math.PI*2); ctx2.fill(); });
        }

        function drawFlower(t) {
            var r = 22;
            ctx2.strokeStyle = 'rgba(70,200,220,0.38)'; ctx2.lineWidth = 0.6;
            ctx2.beginPath(); ctx2.arc(0,0,r,0,Math.PI*2); ctx2.stroke();
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+t*0.05; ctx2.beginPath(); ctx2.arc(Math.cos(a)*r,Math.sin(a)*r,r,0,Math.PI*2); ctx2.stroke(); }
            ctx2.strokeStyle='rgba(70,220,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var j=0;j<6;j++){ var a2=(j/6)*Math.PI*2+t*0.05; var p=[Math.cos(a2)*r,Math.sin(a2)*r]; if(j===0)ctx2.moveTo(p[0],p[1]); else ctx2.lineTo(p[0],p[1]); }
            ctx2.closePath(); ctx2.stroke();
        }

        function drawMetatron(t) {
            var r = 16, ir = r, or2 = r * 2;
            var cs = [[0,0]];
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+t*0.03; cs.push([Math.cos(a)*ir,Math.sin(a)*ir]); }
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+Math.PI/6+t*0.03; cs.push([Math.cos(a)*or2,Math.sin(a)*or2]); }
            ctx2.strokeStyle='rgba(70,180,220,0.28)'; ctx2.lineWidth=0.5;
            cs.forEach(function(c){ ctx2.beginPath(); ctx2.arc(c[0],c[1],r,0,Math.PI*2); ctx2.stroke(); });
            ctx2.strokeStyle='rgba(70,220,170,0.45)'; ctx2.lineWidth=0.5;
            for(var a=0;a<cs.length;a++) for(var b=a+1;b<cs.length;b++){ ctx2.beginPath(); ctx2.moveTo(cs[a][0],cs[a][1]); ctx2.lineTo(cs[b][0],cs[b][1]); ctx2.stroke(); }
            ctx2.fillStyle='rgba(70,220,170,0.7)';
            cs.forEach(function(c){ ctx2.beginPath(); ctx2.arc(c[0],c[1],1,0,Math.PI*2); ctx2.fill(); });
        }

        function drawVesica(t) {
            var r=24, d=r*0.6;
            ctx2.save(); ctx2.rotate(t*0.02);
            ctx2.strokeStyle='rgba(70,200,220,0.45)'; ctx2.lineWidth=0.7;
            ctx2.beginPath(); ctx2.arc(-d/2,0,r,0,Math.PI*2); ctx2.stroke();
            ctx2.beginPath(); ctx2.arc(d/2,0,r,0,Math.PI*2); ctx2.stroke();
            ctx2.strokeStyle='rgba(70,220,170,0.75)'; ctx2.lineWidth=0.8;
            var half=Math.acos(d/(2*r));
            ctx2.beginPath(); ctx2.arc(-d/2,0,r,-half,half); ctx2.arc(d/2,0,r,Math.PI-half,Math.PI+half); ctx2.closePath(); ctx2.stroke();
            ctx2.restore();
        }
        function drawSpiral(t) {
            var maxR=38, turns=4;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.7;
            ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var a=f*turns*Math.PI*2+t*0.05; var r=f*maxR; if(i===0)ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r); else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,190,220,0.35)'; ctx2.lineWidth=0.5;
            ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var a=f*turns*Math.PI*2+t*0.05+Math.PI; var r=f*maxR; if(i===0)ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r); else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r); }
            ctx2.stroke();
        }
        function drawLissajous(t) {
            var a=3, b=2, R=38;
            ctx2.strokeStyle='rgba(70,210,170,0.60)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=400;i++){ var phi=(i/400)*Math.PI*2; var x=R*Math.sin(a*phi+t*0.04); var y=R*Math.sin(b*phi); if(i===0)ctx2.moveTo(x,y); else ctx2.lineTo(x,y); }
            ctx2.stroke();
        }
        function drawStar5(t) {
            var r=36, ir=14, pts=5, rot=t*0.04;
            ctx2.strokeStyle='rgba(70,210,170,0.55)'; ctx2.lineWidth=0.7;
            ctx2.beginPath();
            for(var i=0;i<=pts*2;i++){ var rad=(i%2===0)?r:ir; var a=(i/(pts*2))*Math.PI*2+rot-Math.PI/2; if(i===0)ctx2.moveTo(Math.cos(a)*rad,Math.sin(a)*rad); else ctx2.lineTo(Math.cos(a)*rad,Math.sin(a)*rad); }
            ctx2.closePath(); ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.38)'; ctx2.lineWidth=0.6;
            ctx2.beginPath();
            for(var i=0;i<=pts;i++){ var a=(i/pts)*Math.PI*2+rot-Math.PI/2; if(i===0)ctx2.moveTo(Math.cos(a)*ir,Math.sin(a)*ir); else ctx2.lineTo(Math.cos(a)*ir,Math.sin(a)*ir); }
            ctx2.closePath(); ctx2.stroke();
            var op=[]; for(var i=0;i<pts;i++){ var a=(i/pts)*Math.PI*2+rot-Math.PI/2; op.push([Math.cos(a)*r,Math.sin(a)*r]); }
            ctx2.strokeStyle='rgba(70,220,170,0.28)'; ctx2.lineWidth=0.5;
            for(var i=0;i<pts;i++){ var j=(i+2)%pts; ctx2.beginPath(); ctx2.moveTo(op[i][0],op[i][1]); ctx2.lineTo(op[j][0],op[j][1]); ctx2.stroke(); }
        }
        function drawTorus(t) {
            var R=22, r=10, rings=8;
            for(var i=0;i<rings;i++){ var phi=(i/rings)*Math.PI*2+t*0.03; var cx=Math.cos(phi)*R, cy=Math.sin(phi)*R*0.35; var sc=0.5+0.5*Math.abs(Math.cos(phi)); var al=(0.3+0.3*Math.abs(Math.cos(phi))).toFixed(2); ctx2.strokeStyle='rgba(70,210,170,'+al+')'; ctx2.lineWidth=0.6; ctx2.beginPath(); ctx2.ellipse(cx,cy,r*sc,r*0.35,phi,0,Math.PI*2); ctx2.stroke(); }
            ctx2.strokeStyle='rgba(70,200,220,0.45)'; ctx2.lineWidth=0.7;
            ctx2.beginPath(); ctx2.ellipse(0,0,R+r,(R+r)*0.35,0,0,Math.PI*2); ctx2.stroke();
            ctx2.beginPath(); ctx2.ellipse(0,0,R-r,(R-r)*0.35,0,0,Math.PI*2); ctx2.stroke();
        }
        function drawHelix(t) {
            var R=14, turns=3;
            ctx2.lineWidth=0.8;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05; var y=f*56-28; if(i===0)ctx2.moveTo(Math.cos(ang)*R,y); else ctx2.lineTo(Math.cos(ang)*R,y); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,190,220,0.50)'; ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05+Math.PI; var y=f*56-28; if(i===0)ctx2.moveTo(Math.cos(ang)*R,y); else ctx2.lineTo(Math.cos(ang)*R,y); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,220,170,0.22)'; ctx2.lineWidth=0.5;
            for(var i=0;i<=300;i+=25){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05; var y=f*56-28; ctx2.beginPath(); ctx2.moveTo(Math.cos(ang)*R,y); ctx2.lineTo(Math.cos(ang+Math.PI)*R,y); ctx2.stroke(); }
        }

        function drawRose(t) {
            var R=38, k=3;
            ctx2.strokeStyle='rgba(70,210,170,0.70)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=720;i++){
                var th=(i/720)*Math.PI*2;
                var r=R*Math.cos(k*(th+t*0.018));
                if(i===0) ctx2.moveTo(r*Math.cos(th),r*Math.sin(th));
                else ctx2.lineTo(r*Math.cos(th),r*Math.sin(th));
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.4;
            ctx2.beginPath(); ctx2.arc(0,0,R,0,Math.PI*2); ctx2.stroke();
        }
        function drawCardioid(t) {
            var a=18, rot=t*0.012;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=360;i++){
                var th=(i/360)*Math.PI*2;
                var r=a*(1+Math.cos(th));
                if(i===0) ctx2.moveTo(r*Math.cos(th+rot),r*Math.sin(th+rot));
                else ctx2.lineTo(r*Math.cos(th+rot),r*Math.sin(th+rot));
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,a*2,0,Math.PI*2); ctx2.stroke();
        }
        function drawAsteroid(t) {
            var R=36, rot=t*0.02;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=360;i++){
                var p=(i/360)*Math.PI*2;
                var x=R*Math.pow(Math.cos(p+rot),3), y=R*Math.pow(Math.sin(p+rot),3);
                if(i===0) ctx2.moveTo(x,y); else ctx2.lineTo(x,y);
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.25)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,R,0,Math.PI*2); ctx2.stroke();
            for(var i=0;i<4;i++){
                var a=(i/4)*Math.PI*2+rot;
                ctx2.strokeStyle='rgba(70,220,170,0.18)'; ctx2.lineWidth=0.4;
                ctx2.beginPath(); ctx2.moveTo(0,0); ctx2.lineTo(Math.cos(a)*R,Math.sin(a)*R); ctx2.stroke();
            }
        }
        function drawEpicycloid(t) {
            var Rc=24, rc=8, rot=t*0.015;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=600;i++){
                var p=(i/600)*Math.PI*2+rot;
                var x=(Rc+rc)*Math.cos(p)-rc*Math.cos((Rc/rc+1)*p);
                var y=(Rc+rc)*Math.sin(p)-rc*Math.sin((Rc/rc+1)*p);
                if(i===0) ctx2.moveTo(x,y); else ctx2.lineTo(x,y);
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,Rc,0,Math.PI*2); ctx2.stroke();
        }
        function drawWeb(t) {
            var rings=5, spokes=8, maxR=40, rot=t*0.01;
            ctx2.strokeStyle='rgba(70,200,220,0.35)'; ctx2.lineWidth=0.5;
            for(var s=0;s<spokes;s++){
                var a=(s/spokes)*Math.PI*2+rot;
                ctx2.beginPath(); ctx2.moveTo(0,0); ctx2.lineTo(Math.cos(a)*maxR,Math.sin(a)*maxR); ctx2.stroke();
            }
            ctx2.strokeStyle='rgba(70,210,170,0.55)'; ctx2.lineWidth=0.7;
            for(var ri=1;ri<=rings;ri++){
                var r=(ri/rings)*maxR;
                ctx2.beginPath();
                for(var s=0;s<=spokes;s++){
                    var a=(s/spokes)*Math.PI*2+rot;
                    if(s===0) ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r);
                    else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r);
                }
                ctx2.closePath(); ctx2.stroke();
            }
        }

        function geoFrame() {
            ctx2.clearRect(0,0,S,S);
            ctx2.save(); ctx2.translate(S/2,S/2);
            angle += 0.007;
            if      (key==='flower')     drawFlower(angle);
            else if (key==='metatron')   drawMetatron(angle);
            else if (key==='vesica')     drawVesica(angle);
            else if (key==='spiral')     drawSpiral(angle);
            else if (key==='lissajous')  drawLissajous(angle);
            else if (key==='star5')      drawStar5(angle);
            else if (key==='torus')      drawTorus(angle);
            else if (key==='helix')      drawHelix(angle);
            else if (key==='rose')       drawRose(angle);
            else if (key==='cardioid')   drawCardioid(angle);
            else if (key==='asteroid')   drawAsteroid(angle);
            else if (key==='epicycloid') drawEpicycloid(angle);
            else if (key==='web')        drawWeb(angle);
            else                         drawWire(shape, angle*0.4, angle);
            ctx2.restore();
            rafId = requestAnimationFrame(geoFrame);
        }
        geoFrame();

        // ── Scrolling element report text (right of canvas) ────────────────
        var reportEl = document.createElement('div');
        reportEl.className = 'cbg-geo-report';
        var innerEl = document.createElement('div');
        innerEl.className = 'cbg-geo-report-inner';
        reportEl.appendChild(innerEl);
        bodyEl.appendChild(reportEl);

        var lineH = 7.7; // px per line: 0.33rem * 16px base * 1.45 line-height ≈ 7.66
        var visCount = Math.ceil(110 / lineH) + 3; // lines to fill viewport + 3 lookahead
        var reportBuf = [];
        for (var ri = 0; ri < visCount + 2; ri++) { reportBuf.push(pick(GEO_REPORT_LINES)); }
        innerEl.textContent = reportBuf.join('\n');
        var scrollOff = 0;
        var scrollTmr = setInterval(function () {
            scrollOff += 0.35;
            if (scrollOff >= lineH) {
                scrollOff -= lineH;
                reportBuf.shift();
                reportBuf.push(pick(GEO_REPORT_LINES));
                innerEl.textContent = reportBuf.join('\n');
            }
            // Negative Y: content scrolls upward, new lines enter from below
            innerEl.style.transform = 'translateY(-' + scrollOff.toFixed(1) + 'px)';
        }, 60);

        var ttl = rand(4000, 9000);
        setTimeout(function () {
            clearInterval(scrollTmr);
            cancelAnimationFrame(rafId);
            win.classList.add('cbg-win--out');
            setTimeout(function () { if (win.parentNode) win.parentNode.removeChild(win); }, 500);
        }, ttl);
    }

    // ── Corporate memo intercept ──────────────────────────────────────────────

    var MEMOS = [
        'CLASSIFIED // EYES ONLY\nTO: VP Asset Security\nFROM: Board Dir. 94-C\nRE: Unit 7-W — Liability\n\nProceed w/ scheduled termination.\nFull blackout per Protocol 9.\nNo media. No record. Confirm 0300.\nAuth: 0x9f3a\n\n>>> GLMZ RELAY INTERCEPT <<<',
        'INTERNAL — DELETE AFTER READ\nTO: District Enforcement\nRE: Freelancer File #3301\n\nFlag for immediate termination.\nAssociates: Annex B.\nOffer suppress — THEN terminate.\nSilent bounty: Φ40,000 corp rate.\n\n>>> INTERCEPT :: dist12/relay <<<',
        'FROM: Crest Dynamics Legal\nTO: Subsidiary Compliance\nRE: Neural Data Harvest — Batch 7\n\nHarvest neural-sig per Q3 plan.\nConsent waiver buried in ToS §44-F.\nDo NOT flag to district oversight.\nDelete harvest logs after 72h.\n\n[RELAY CORRUPTION — partial recv]',
        'PRIORITY: URGENT\nTO: Security Chief, Dist 9\nRE: Witness Mgmt — Case #0091\n\nAll three witnesses — silence them.\nPreferred: reassignment.\nFallback: Protocol Null.\nContractor already briefed.\n\n[SIG: 0xdc3545 // VAULTDROP]',
        'FROM: Acquisition Strategy\nTO: Field Operations\nRE: Sector 4 Hostile Takeover\n\nPhase 1: Destroy competitor supply.\nPhase 2: Corner remaining market.\nPhase 3: Price floor +400%.\nKeep district enforcement on payroll.\n\n>>> DARK NODE :: glmz/7 <<<',
        'TO: Behavioral Analytics Team\nRE: Pop. Compliance — Batch 9\n\nDeploy sublim. seq. in dist2 feed.\nTarget: dissent suppression.\nVector: entertainment network.\nDeny if queried. Log: none.\n\n[INTERCEPT CONFIDENCE: 0.88]',
        'PRIVILEGED COMMUNICATION\nFROM: Acquisitions Div. 3\nTO: Field Security\nRE: Competitor Asset — Terminal\n\nSubject refused acquisition offer.\nAuthorize final resolution.\nRecover IP before cleanup.\nRoute via dark node only.\n\n[GLMZ INTERCEPT: 94%]',
        'FROM: Behavioral Mod. Group\nTO: Neural Interface Program\nRE: Opt-Out Handling — Priority\n\nOperators flagging consent removal:\nDo NOT honor. Flag as compromised.\nAccelerate neural-key binding.\nLegal has pre-approved language.\n\n>>> CREST DYNAMICS INTERNAL <<<',
        'URGENT — ALL DISTRICT COMMANDERS\nFROM: Enforcement Central\nRE: Freelancer Surge — District 7\n\nSurge is cover. Target is #3301.\nCollateral: acceptable up to 40%.\nNo press. No GLMZ incident report.\nClose window before 0600.\n\n[AUTH: 0xBE0441]',
        'FROM: Data Harvesting Unit 9\nTO: Board — Eyes Only\nRE: Q3 Neural Data — Profit\n\n44,000 operator profiles sold.\nBuyer: NeuralState consortium.\nData stripped of ID — plausibly.\nReturn: Φ4.4M corp rate.\n\n>>> RELAY CORRUPTION <<<',
        'TO: Corp Liaison, District 9\nFROM: Legal Stratagem\nRE: Liability Suppression\n\nSeven incident reports — suppress.\nSettle: Φ3,000 each, no admission.\nIf refused: standard protocol 7.\nDestroy original filings after.\n\n[INTERCEPT: dist9/dark-node]',
        'FROM: Medical Ethics Bypass\nTO: Cyberware Division\nRE: Non-Consenting Subjects\n\nTrial cohort 44 is involuntary.\nClinical oversight circumvented.\nOutcomes trending positive.\nTerminate failed cohort quietly.\n\n>>> DARK NODE :: dist4 <<<',
        'CLASSIFIED — BOARD LEVEL ONLY\nFROM: Asset Liquidation\nTO: Director 94-C\nRE: Witness List — Case #0099\n\nSix witnesses. Five located.\nSchedule: sequential, 72h window.\nMake them look accidental.\nFinal — offer Φ80k, else same.\n\n[SIG: 0x9A3301 // VERIFIED]',
        'FROM: Media Relations (Covert)\nTO: Entertainment Network\nRE: Narrative Seeding — Phase 3\n\nInsert: freelancers = terrorists.\nEnforcement = civic guardians.\nSubtlety required — 6mo campaign.\nDeny corp authorship at all costs.\n\n[RELAY: glmz-comms-d12]',
        'TO: Corp Security, District 4\nFROM: Territorial Division\nRE: Hostile Freelancer — 0x4492\n\nOperator has evidence of Q3 harvest.\nContain before they reach press.\nLevel 3 protocol authorized.\nNo record of this transmission.\n\n>>> RELAY CORRUPTION — partial <<<',
        'FROM: Population Control Div.\nTO: GLMZ District Administrators\nRE: Food Access Throttle — Batch 4\n\nDistrict 9 rationing at 44%.\nReduction to 30% approved Q1.\nFrame as supply chain failure.\nTrack compliance via BCI metrics.\n\n[INTERCEPT CONFIDENCE: 0.91]',
        'INTERNAL — DESTROY AFTER READ\nFROM: Corp Ethics Committee\nTO: [REDACTED]\nRE: Upcoming Ethics Review\n\nAnswers to inquiries 3, 7, 12:\n— Data: "aggregated, anonymized"\n— Consent: "implied via ToS"\n— Deaths: "within projections"\nMembers have been briefed.\n\n[SIG: 0xDEAD4412]',
        // 15 more
        'FROM: Extraction Unit 7\nTO: Field Ops Director\nRE: Asset 3301 — Status Update\n\nAsset uncooperative after 72h.\nStandard persuasion ineffective.\nPhase 2 authorized by Dir. 94-C.\nDispose after extraction complete.\n\n>>> GLMZ DARK NODE :: dist4 <<<',
        'FROM: Neural Analytics Board\nTO: BCI Program Dir.\nRE: Operator 0x4492 — Classify\n\nNeural profile matches dissident tag.\nRecommend silent reclassification.\nAccess throttle: 40% — covert.\nDo not inform subject.\n\n[RELAY: sec/vault-7]',
        'CONFIDENTIAL — NO EXTERNAL\nFROM: Subsidiary Relations\nTO: Enforcement Liaison\nRE: Competitor Infrastructure\n\nThree relay nodes confirmed hostile.\nCoordinate with dist9 enforcement.\nPlausible denial required.\nNo Crest equipment — freelancers.\n\n>>> INTERCEPT :: corp/mirror <<<',
        'TO: Narrative Ops Team\nFROM: Social Influence Div.\nRE: Freelancer Problem — Framing\n\nCurrent narrative: economic threat.\nProposed pivot: public safety.\nTimeline: 3-week push via ent/net.\nSuccess metric: 60% public favor.\n\n[GLMZ RELAY: dist12/comms]',
        'INTERNAL ONLY — LEGAL\nFROM: Compliance Division\nTO: Security Dir.\nRE: Incident 0091 — Paperwork\n\nThree deaths out of scope.\nFile as industrial accident.\nFamily settlements: Φ8,000 each.\nNDA required — enforce aggressively.\n\n[SIG: 0x9A3301 // CORP-LEGAL]',
        'FROM: Behavioral Modification R&D\nTO: Program Board\nRE: Trial Cohort 7 — Outcomes\n\nCompliance rate: 88% (target 80%).\nSubjects unaware of BCI seeding.\nSide effects: within tolerance.\nProceed to Batch 8 — 500 subjects.\n\n>>> DARK NODE :: dist7/relay <<<',
        'URGENT PRIORITY\nFROM: Intelligence Operations\nTO: Crest Dynamics\nRE: Journalist — Case #0441\n\nJournalist has partial Q3 data.\nSourced from inside — find leak.\nContain story before press cycle.\nPermanent solution if necessary.\n\n[INTERCEPT CONFIDENCE: 0.96]',
        'FROM: Territorial Expansion\nTO: Legal + Enforcement\nRE: District 2 Consolidation\n\nPhase 1 complete: 3 orgs dissolved.\nPhase 2: Purchase remaining assets.\nPhase 3: Restructure workforce.\nExpected redundancies: 400-600.\n\n>>> CORP/BROKER :: VAULTDROP <<<',
        'CLASSIFIED — ABOVE TOP\nFROM: AI Autonomy Division\nTO: Board Only\nRE: Behemoth Meridian-88 — Update\n\nAutonomy module fully deployed.\nHuman oversight: symbolic only.\nContingency removal: scheduled.\nDo not log this meeting.\n\n[SIG: 0xFF0000 // PURGE-ON-READ]',
        'TO: Forensic Suppression Team\nFROM: Director 94-C\nRE: Evidence — Batch 0099\n\nSeven files. Delete originals.\nOverwrite media 3 passes.\nPurge relay cache: dist4, dist7.\nConfirm by 0300 UTC.\n\n>>> RELAY CORRUPTION — terminal <<<',
        'FROM: Public Health Proxy\nTO: District Supply Chain\nRE: Pharmaceutical Diversion\n\nDivert Batch 44 to compliance stream.\nReduce district 9 access 60%.\nFrame as shortage — corp approved.\nProfits to hidden account 0x9F3A.\n\n[INTERCEPT: glmz/gate-12]',
        'INTERNAL — NO EXTERNAL\nFROM: BCI Surveillance Unit\nTO: Corp Intelligence\nRE: Watchlist Update — Q2\n\n3,301 operators under passive monitor.\n412 flagged for attention.\nNeural-key patterns attached.\nAutomated escalation if triggered.\n\n>>> CREST INTERNAL :: sec/enclave <<<',
        'TO: Field Security, District 12\nFROM: Asset Protection\nRE: Freelancer Collective — Action\n\nCell identified: 4 members, dark-node.\nSurveillance complete: 18 days.\nAuthorize simultaneous termination.\nCoordinate with dist9 — 0400.\n\n[SIG: 0xBE0441 // SILENT]',
        'FROM: Data Monetization Group\nTO: Board of Directors\nRE: Neural Signature Auction — Q3\n\n88,000 unique profiles ready.\nSale to NeuralState: Φ8.8M.\nAnonymization: cosmetic only.\nAudit-proof — legal reviewed.\n\n>>> RELAY: corp/mirror-d7 <<<',
        'PRIORITY ALPHA\nFROM: Enforcement Central\nTO: ALL DISTRICT COMMANDS\nRE: Operation Dark Census\n\nRound up all unregistered operators.\nBCI registration mandatory by 0600.\nNon-compliance: level 3 detention.\nMedia blackout in effect.\n\n[RELAY CORRUPTION — CRITICAL]',
        // 100 new entries
        'FROM: BCI Compliance Division\nTO: Field Operations\nRE: Operator 0x8001 — Monitor\n\nProfile match: dissent cluster B-9.\nPassive monitor activated — covert.\nDo not alert subject or associates.\nReport threshold: 3 anomalies/week.\n\n>>> CREST INTERNAL :: bci/mon <<<',
        'CONFIDENTIAL — NO EXTERNAL\nFROM: Territory Acquisitions\nTO: Legal + Enforcement\nRE: District 2 Lease Buyout\n\nCurrent occupants: 340 households.\nRelocation budget: Φ0 — evict.\nTimeline: 90 days. Legal cover: ready.\nEnforce at day 91 — no media.\n\n[INTERCEPT: glmz/gate-2]',
        'FROM: Neural Surveillance Unit\nTO: Analytics Board\nRE: Mass BCI Passive Read — Q4\n\n9,441 operators read without consent.\nData sold to 3 corps: anonymized.\nConsent assumed via Crest ToS §88.\nArchive raw data — 72h then purge.\n\n>>> DARK NODE :: dist9/relay <<<',
        'INTERNAL — DESTROY AFTER READING\nFROM: Incident Response Lead\nTO: Board Dir. 94-C\nRE: Witness — Case #0441\n\nWitness has partial comms logs.\nOffer: Φ12,000 NDA. Deadline: 48h.\nFallback: standard Protocol 7.\nDo not involve district enforcement.\n\n[SIG: 0xBE0441 // PURGE-ON-READ]',
        'FROM: Behavioral Modification R&D\nTO: Corp Ethics Bypass Board\nRE: Trial Cohort 9 — Consent\n\nCohort 9: 600 operators. No consent.\nBCI seeding active since Week 1.\nSide effects: within redline 4/600.\nProceed Batch 10 — 1,000 subjects.\n\n>>> RELAY CORRUPTION — partial <<<',
        'TO: Corp Territorial Division\nFROM: Field Operations\nRE: Freelancer Cell — District 7\n\nCell confirmed: 6 members.\nMesh relay map attached (encrypted).\nRecommend simultaneous containment.\nUse dark-node for coord — no log.\n\n[INTERCEPT CONFIDENCE: 0.94]',
        'FROM: Legal Stratagem Group\nTO: Compliance Director\nRE: Incident #0812 — Suppress\n\nNine injuries, two fatal. No press.\nFamily payouts: Φ4,000 each. NDA.\nIf refused: standard Protocol 7.\nFund from slush account 0x9F3A.\n\n>>> GLMZ RELAY :: corp/mirror <<<',
        'CLASSIFIED — BOARD ONLY\nFROM: AI Ethics Bypass Committee\nTO: Behemoth Program Director\nRE: Meridian-88 — Human Override\n\nHuman kill-switch: deactivated Q3.\nAudit trail: sanitized this date.\nDo not brief district enforcement.\nThis memo self-destructs in 24h.\n\n[SIG: 0xFF0000 // EXECUTIVE]',
        'FROM: Media Narrative Division\nTO: Entertainment Network Ops\nRE: Freelancer Rebranding Phase 4\n\nPhase 3 achieved 58% public approval.\nPhase 4: link freelancers to flooding.\nTimeline: 8-week saturation campaign.\nBury counter-narrative algorithmically.\n\n[RELAY: glmz-comms-d4]',
        'TO: Data Monetization Board\nFROM: BCI Product Division\nRE: Neural Profile Batch 10 — Sale\n\n120,000 unique neural profiles ready.\nBuyer: NeuralState Consortium bid Φ12M.\nAnonymization: cosmetic — re-linkable.\nClose deal before Q4 audit window.\n\n>>> CORP/BROKER :: VAULTDROP <<<',
        'FROM: District 9 Enforcement\nTO: Corp Security Central\nRE: Op Blackout — Freelancer #4492\n\nSubject has evidence of Batch 7 sale.\nContain before they reach safe-house.\nLevel 4 authorized — no witnesses.\nConfirm via dark-node by 0400.\n\n[SIG: 0x9A4412 // SILENT]',
        'FROM: Supply Chain Division\nTO: District 4 Fabrication\nRE: Crest Key Diversion — Batch 3\n\nDivert 200 Crest neural keys.\nDestination: unlicensed resale channel.\nMargin: 400% over corp rate.\nLog as defective — destroy paperwork.\n\n>>> INTERCEPT :: dist4/fab <<<',
        'INTERNAL — LEGAL PRIVILEGE\nFROM: Corp Ethics Committee (Actual)\nTO: [REDACTED]\nRE: Upcoming External Audit — Prep\n\nAnswers to questions 4, 8, 15:\n— BCI reads: "consented aggregated"\n— Deaths: "within actuarial range"\n— Profiling: "anonymized product"\nMembers receive standard briefing.\n\n[SIG: 0xDEAD9441]',
        'FROM: Extraction Team Bravo\nTO: Asset Recovery Director\nRE: Op Recovery — Target 0x7712\n\nTarget has corp comms archive 4.2GB.\nPhase 1: social engineering — 72h.\nPhase 2: forced extraction if fail.\nDispose after recovery. No record.\n\n[INTERCEPT: dist7/dark-node]',
        'PRIORITY URGENT\nFROM: Intelligence Division\nTO: Crest Dynamics Board\nRE: Press Contact — Case #0812\n\nJournalist sourced from employee 3.\nEmployee 3 identified — terminate.\nStory kill: direct legal pressure.\nIf press publishes: deny + counter.\n\n>>> GLMZ DARK NODE :: corp/sec <<<',
        'FROM: Population Analytics Team\nTO: GLMZ Infrastructure Division\nRE: Food Throttle — Batch 6\n\nDistrict 7 ration reduction 45→30%.\nFrame: supply chain disruption.\nCompliance tracking via BCI data.\nExpect protest — enforce at day 14.\n\n[INTERCEPT CONFIDENCE: 0.89]',
        'CLASSIFIED — DESTROY AFTER READ\nFROM: Forensic Suppression Unit\nTO: Dir. 94-C\nRE: Evidence — Case #0441\n\nEleven files. Delete originals now.\n3-pass overwrite on all media.\nPurge relay cache: d4, d7, d9, d12.\nConfirm destruction by 0500 UTC.\n\n>>> RELAY CORRUPTION — terminal <<<',
        'FROM: Pharmaceutical Division\nTO: District Supply Proxy\nRE: Med Diversion — Batch 88\n\nDivert pain meds from District 9.\nRedirect to corp-affiliate at 300%.\nFrame as logistics failure — press ok.\nProfits: hidden account 0x3AF7.\n\n[RELAY: glmz/gate-9]',
        'TO: Corp Security, All Districts\nFROM: Intelligence Operations\nRE: Dark Census — Expansion\n\nExpand unregistered operator sweep.\nCollect: biometric, BCI, location.\nStore: dark archive 0x9F3A.\nLegal cover: non-existent — proceed.\n\n>>> CREST INTERNAL :: sec/vault <<<',
        'FROM: Territorial Acquisition Div.\nTO: Field Operations, District 4\nRE: Hostile Freelancer Collective\n\nCollective confirmed: 9 members.\n4 have corp comms archive fragments.\nSimultaneous containment authorized.\n0300 window — no enforcement record.\n\n[SIG: 0xBE0441 // SILENT]',
        'FROM: BCI Product Division\nTO: Analytics Board\nRE: Passive Neural Read — Expansion\n\nExpand passive read to District 12.\nTarget: 4,000 operators — no consent.\nData pipeline to NeuralState open.\nPurge logs 72h post-collection.\n\n>>> DARK NODE :: dist12/relay <<<',
        'CONFIDENTIAL — EYES ONLY\nFROM: Legal Suppression Team\nTO: Corp General Counsel\nRE: Class Action — Pre-empt\n\nSeven plaintiffs — silence them.\nOffer: Φ8,000 each + NDA strict.\nIf refused: standard Protocol 7.\nDo not create discoverable record.\n\n[SIG: 0x9A3301 // LEGAL]',
        'FROM: Narrative Control Group\nTO: Social Influence Division\nRE: District 9 Unrest — Counter\n\nUnrest framed as criminal activity.\nVectors: ent/net, glmz/comms.\nTarget: 70% public disapproval.\n6-week push — subtle, deniable.\n\n[RELAY: corp/mirror-d9]',
        'TO: All District Commanders\nFROM: Corp Security Central\nRE: Freelancer Watchlist Update\n\n412 operators added to watchlist.\nPassive BCI monitor: auto-activated.\nEscalation trigger: 3 anomalies.\nCoordinate silently — no warrants.\n\n>>> CREST INTERNAL :: corp/audit <<<',
        'FROM: Medical Division\nTO: BCI Trial Board\nRE: Non-Consenting Cohort 12\n\nCohort 12: 800 subjects — involuntary.\nNeural seeding active Week 1.\nFailed subjects (n=8): disposed.\nProceed Cohort 13 — 1,500 subjects.\n\n>>> DARK NODE :: dist4 <<<',
        'PRIORITY ALPHA\nFROM: Corp Board\nTO: All Division Heads\nRE: Op Silent Horizon — Phase 2\n\nPhase 1 complete: 9,441 profiled.\nPhase 2: behavioral modification live.\nSubjects unaware — 88% compliance.\nPhase 3 brief: eyes only, next meeting.\n\n[SIG: 0xFF0000 // BOARD EYES ONLY]',
        'FROM: Intelligence Operations\nTO: Field Security, District 12\nRE: Journalist Network — Terminate\n\nNetwork: 4 journalists, 2 sources.\nAll have partial Batch 7 records.\nSimultaneous containment authorized.\n0400 UTC — dark-node coord only.\n\n[INTERCEPT CONFIDENCE: 0.97]',
        'FROM: BCI Compliance (Covert)\nTO: Analytics Board\nRE: Dissent Profiling — Batch 4\n\n3,301 operators profiled for dissent.\n441 flagged for close monitor.\nNeural suppression: 3 subjects active.\nDo not brief district oversight.\n\n>>> CREST INTERNAL :: bci/cls <<<',
        'INTERNAL — NO EXTERNAL DISTRIBUTION\nFROM: Population Control Division\nTO: Behavioral Analytics\nRE: Compliance Vector — Phase 4\n\nPhase 4: water supply additive.\nVector: district 9, 4 reservoirs.\nEffect: reduce dissent 60%.\nDeniable — keep corp link dark.\n\n[INTERCEPT: glmz/gate-9]',
        'FROM: Acquisition Strategy\nTO: Territory Director\nRE: District 2 Hostile Consolidation\n\n4 orgs dissolved. 2 remaining.\nRemaining orgs — offer, then force.\nExpected job losses: 800-1,200.\nFrame: restructuring for efficiency.\n\n>>> CORP/BROKER :: dist2/comms <<<',
        'CLASSIFIED — ABOVE SECRET\nFROM: Autonomy Division\nTO: Behemoth Program Lead\nRE: Meridian-88 — Override Module\n\nHuman override fully disabled Q4.\nContingency removal: complete.\nAudit trail: sanitized.\nMeridian-88 now operates autonomous.\n\n[SIG: 0xFF0000 // PURGE-ON-READ]',
        'FROM: Corp Ethics (Real)\nTO: [REDACTED]\nRE: Audit Preparation — Q4\n\nAll anomalous entries normalized.\nDeaths filed as industrial accidents.\nNeural harvest marked as consented.\nAuditors pre-briefed — routine only.\n\n[SIG: 0xDEAD4412 // CORP-LEGAL]',
        'TO: Field Operations, District 4\nFROM: Asset Protection Division\nRE: Freelancer 0x1187 — Action\n\nOperator has relay map + audit gap data.\nContain before safe-house in dist2.\nLevel 3 authorized. No witnesses.\nConfirm via vault-drop, not relay.\n\n[INTERCEPT: dist4/dark-node]',
        'FROM: Data Monetization (Covert)\nTO: NeuralState Consortium\nRE: Batch 11 — Delivery Confirm\n\n180,000 neural profiles delivered.\nAnonymization: re-linkable — caveat.\nPayment: Φ18M to account 0x9F3A.\nPurge transfer logs — 24h window.\n\n>>> RELAY CORRUPTION — partial <<<',
        'FROM: Legal Stratagem Group\nTO: Corp Security Dir.\nRE: Incident #1187 — Paperwork\n\nFive deaths. File as system failure.\nFamilies: Φ6,000 each + strict NDA.\nIf refused: standard Protocol 7.\nDestroy original incident reports.\n\n[SIG: 0x9A3301 // CORP-LEGAL]',
        'PRIORITY URGENT\nFROM: Corp Security Central\nTO: District 9 Enforcement\nRE: Journalist — Final Contact\n\nJournalist has full Batch 7 archive.\nAll previous offers refused.\nAuthorize permanent containment.\nRoute through dark-node only.\n\n>>> GLMZ DARK NODE :: corp/sec <<<',
        'FROM: BCI Product (Internal)\nTO: Behavioral Mod Group\nRE: Neural Suppression — Scale\n\nSuppress active: 12 subjects.\nEffectiveness: 88% compliance.\nSide effects: 2 seizures — tolerated.\nScale to 100 subjects next quarter.\n\n[RELAY: sec/vault-9]',
        'TO: Corp Territory Division\nFROM: Field Security Director\nRE: District 12 Consolidation\n\nPhase 1: 6 orgs dissolved.\nPhase 2: remaining 3 — force buy.\nExpected redundancies: 600-900.\nEnforcement on retainer — no record.\n\n>>> INTERCEPT :: corp/broker <<<',
        'FROM: Population Analytics Division\nTO: GLMZ Infrastructure\nRE: BCI Geofencing — Batch 3\n\nGeofence live for 1,200 operators.\nTriggered containment: 44 events.\nFalse positive rate: 12% — tolerated.\nDo not brief district legal.\n\n[INTERCEPT CONFIDENCE: 0.92]',
        'CONFIDENTIAL — CORP EYES ONLY\nFROM: Behavioral Mod R&D\nTO: Neural Interface Program\nRE: Consent Removal Handling\n\n440 operators revoked consent Q3.\nRevocation: logged but not honored.\nNeural seeding continues per plan.\nLegal cover: ToS §88-F override.\n\n>>> CREST INTERNAL :: bci/auth <<<',
        'FROM: Extraction Unit 12\nTO: Asset Recovery Director\nRE: Asset 0x4492 — Phase 2\n\nPhase 1 (social eng.) failed 96h.\nPhase 2: coercive extraction auth.\nRecover: comms archive + neural map.\nDispose after extraction. No record.\n\n[SIG: 0xBE0441 // SILENT]',
        'TO: Corp Board — Eyes Only\nFROM: Autonomy Division\nRE: Meridian-88 — Status\n\nAutonomy index: 1.000 (full).\nHuman oversight: ceremonial only.\nLast human override: 847 days ago.\nContingency shutdown: physically removed.\n\n[SIG: 0xFF0000 // EXECUTIVE-ONLY]',
        'FROM: Supply Chain (Covert)\nTO: District 4 Fab Director\nRE: BIM Chip Diversion — Batch 5\n\nDivert 400 BIM chips to gray market.\nMargin: 350% — slush fund 0x3AF7.\nLog as failed QA — destroy paperwork.\nBuyer: freelancer collective dist7.\n\n>>> DARK NODE :: dist4/fab <<<',
        'INTERNAL — DESTROY AFTER READ\nFROM: Narrative Control (Covert)\nTO: Ent/Net Operations\nRE: Flood Narrative — Phase 2\n\nPhase 1: Missouri flood = freelancers.\nPhase 2: freelancers = corp saboteurs.\nTimeline: 12-week subliminal push.\nCorp authorship: permanently deniable.\n\n[RELAY: corp/mirror-d12]',
        'FROM: Legal Division\nTO: Corp Compliance Director\nRE: GLMZ Human Rights Inquiry\n\nInquiry scope: BCI consent violations.\nResponse strategy: cooperate partially.\nWithhold: Batches 7-11, all ops.\nSettlement range: Φ2M — pre-empt.\n\n>>> INTERCEPT :: corp/audit <<<',
        'TO: Field Security, District 7\nFROM: Intelligence Ops\nRE: Freelancer Cell — 8 Members\n\nCell confirmed: 8 operators.\nMesh network: dark-node relay dist4.\nSurveillance: 22 days complete.\nAuthorize simultaneous containment.\n\n[SIG: 0x9A4412 // SILENT]',
        'FROM: Corp Finance (Covert)\nTO: Account Management\nRE: Slush Fund 0x9F3A — Transfer\n\nTransfer Φ22M to account 0x9F3A.\nSource: neural profile sale Batch 10.\nLaunder via dist4 fabrication subs.\nPurge all transaction records — 24h.\n\n>>> RELAY CORRUPTION — terminal <<<',
        'CLASSIFIED — BOARD LEVEL\nFROM: Intelligence Operations\nTO: Corp Board\nRE: Q4 Neural Harvest — Revenue\n\n320,000 neural profiles sold YTD.\nTotal revenue: Φ32M.\nBuyers: NeuralState, 2 state proxies.\nRisk of exposure: 4% — tolerated.\n\n[INTERCEPT CONFIDENCE: 0.98]',
        'FROM: Behavioral Modification Lead\nTO: Neural Suppression Team\nRE: Scale Authorization — Q4\n\nQ3 results: 88% effectiveness.\nQ4 target: 500 suppression subjects.\nSide effects budget: 12 adverse.\nAuthorization: Dir. 94-C signature.\n\n>>> DARK NODE :: dist9/vault <<<',
        'FROM: Corp Ethics Bypass Board\nTO: Medical Division\nRE: Cohort 14 — Authorization\n\nCohort 14: 2,000 subjects — no consent.\nBCI neural seeding pre-approved.\nClinical oversight: waived.\nOutcomes below 95% compliance: escalate.\n\n[SIG: 0xDEAD4412]',
        'TO: All Division Heads\nFROM: Corp Board\nRE: Op Silent Archive — Phase 3\n\nPhase 1-2 complete. Data secured.\nPhase 3: destroy all paper trails.\nDeadline: 72h. Confirm by vault-drop.\nNon-compliance: Protocol 9.\n\n>>> RELAY: corp/broker :: VAULTDROP <<<',
        'FROM: District 12 Enforcement\nTO: Corp Security Central\nRE: Op Dark Census — Status\n\n3,412 operators processed.\n441 detained, 88 in Protocol 7.\n12 non-compliant — escalate.\nNo media events — blackout holding.\n\n[RELAY: glmz-comms-d12]',
        'FROM: Pharmaceutical Diversion Unit\nTO: Corp Finance\nRE: Q3 Medical Diversion Revenue\n\nDiverted: 44,000 units pain meds.\nRevenue: Φ4.4M to account 0x9F3A.\nFramed: 3 supply chain incidents.\nNo regulatory inquiry — clean.\n\n>>> INTERCEPT :: glmz/gate-12 <<<',
        'CONFIDENTIAL — NO EXTERNAL\nFROM: BCI Surveillance Central\nTO: Analytics Board\nRE: Behavioral Pattern Archive\n\n9,441 operators: full behavioral map.\nPatterns sold to 4 state actors.\nRe-identification risk: 88% — noted.\nDo not archive this communication.\n\n[SIG: 0xBE0441 // CORP-ONLY]',
        'FROM: Legal Suppression Team\nTO: Corp Counsel\nRE: Class Action #0088 — Pre-empt\n\n12 plaintiffs — all have neural data.\nOffer: Φ12,000 each + NDAs.\nIf any refuse: Protocol 7 standard.\nCourt filing blocked — 60-day stay.\n\n[INTERCEPT: corp/legal-d12]',
        'TO: Field Operations, All Districts\nFROM: Enforcement Central\nRE: Freelancer Sweep — Q4 Quota\n\nQ4 quota: 1,200 freelancers processed.\nCurrent count: 441 — accelerate.\nCollateral: acceptable ≤20%.\nMedia blackout: indefinite.\n\n>>> GLMZ RELAY :: corp/enforce <<<',
        'FROM: Intelligence Division\nTO: Corp Security Dir.\nRE: GLMZ Resistance Network\n\nNetwork: 44 operators, 12 nodes.\nConnections to outside press: 3.\nContain network before press contact.\nLevel 4 authorized — no survivors.\n\n[SIG: 0x9A4412 // EXECUTIVE]',
        'FROM: BCI Product Division\nTO: Corp Revenue Board\nRE: Neural Key Resale — Batch 6\n\n600 operator neural keys resold.\nBuyers: 2 state actors, 1 corp proxy.\nMargin: Φ8.8M. Laundered: 0x9F3A.\nSubjects unaware — keep dark.\n\n>>> DARK NODE :: corp/vault <<<',
        'CLASSIFIED — DESTROY ON READ\nFROM: Autonomy Division\nTO: Corp Board Only\nRE: Meridian-88 — Target Protocol\n\nM-88 autonomous targeting active.\nFirst target list: 44 operators.\nNo human review required — designed out.\nThis memo self-destructs in 4 minutes.\n\n[SIG: 0xFF0000 // PURGE-IMMEDIATE]',
        'FROM: Corp Supply (Covert)\nTO: District 9 Fabrication\nRE: RBS Disc Black Market — Batch 2\n\nDivert 300 RBS assemblies.\nBuyer: freelancer supply network dist7.\nMargin: 280% over corp rate.\nDocument as defective — standard cover.\n\n>>> INTERCEPT :: dist9/fab <<<',
        'TO: Corp Board — Eyes Only\nFROM: Behavioral Modification Lead\nRE: Mass Compliance — Phase 5 Brief\n\nPhase 4: 88% compliance achieved.\nPhase 5: scale to all GLMZ districts.\nVector: water, BCI, entertainment.\nTimeline: 18 months. Budget: classified.\n\n[SIG: 0xFF0000 // BOARD ONLY]',
        'FROM: Media Narrative Division\nTO: Entertainment Network (Corp)\nRE: Outside World — Narrative Seal\n\nSeal narrative: outside GLMZ = death.\nVectors: all entertainment channels.\nGoal: zero voluntary exit attempts.\nDeny corp authorship at all costs.\n\n[RELAY: corp/mirror-d7]',
        'FROM: Legal Division\nTO: Corp Compliance\nRE: GLMZ Human Rights Inquiry #2\n\nInquiry escalated — outside press.\nResponse: delay + partial disclosure.\nWithhold: all BCI data, all ops.\nSettlement range: Φ10M — board auth.\n\n>>> INTERCEPT :: corp/audit <<<',
        'PRIORITY URGENT\nFROM: Intelligence Operations\nTO: Corp Security Central\nRE: Leak — Inside Employee #7\n\nEmployee #7 has Batch 9 records.\nIdentified: district 4, night shift.\nContain before next press cycle.\nProtocol 7 authorized if NDA fails.\n\n[INTERCEPT CONFIDENCE: 0.99]',
        'FROM: Territorial Division\nTO: Field Security Director\nRE: District 4 — Final Phase\n\nAll 12 orgs dissolved. Complete.\nFormer operators: 90-day transition.\nNon-compliant at day 91: detained.\nReport to board — mission complete.\n\n>>> CORP/BROKER :: VAULTDROP <<<',
        'FROM: Analytics Board\nTO: BCI Surveillance Central\nRE: Dissent Score Expansion\n\nExpand dissent scoring to 44,000 operators.\nAuto-escalation at score ≥ 0.7.\nEscalation: silent Protocol 7 queue.\nDo not log this expansion order.\n\n[SIG: 0x9A3301 // SILENT]',
        'FROM: Corp Ethics Bypass (Real Minutes)\nTO: [REDACTED]\nRE: Q3 Review — Unredacted\n\nHuman casualties: 22 confirmed.\nAll classified as industrial incident.\nCover holding — press: none.\nVote: proceed Phase 5 — unanimous.\n\n[SIG: 0xDEAD9441]',
        'TO: Enforcement Central\nFROM: Intelligence Division\nRE: Op Dark Census — Expansion B\n\nExpand sweep to districts 2, 4, 9.\nTarget: all unregistered + watchlist.\nCollateral: acceptable ≤ 30%.\nConfirm via vault-drop by 0300.\n\n>>> GLMZ DARK NODE :: corp/enforce <<<',
        'FROM: Supply Chain Division\nTO: Pharmaceutical Diversion Unit\nRE: Q4 Diversion — Authorization\n\nQ4 diversion: 80,000 units approved.\nDistricts: 2, 7, 9 — rotating.\nRevenue target: Φ8M to account 0x3AF7.\nMultiple failure frames ready.\n\n[INTERCEPT: glmz/gate-7]',
        'CLASSIFIED — BOARD LEVEL ONLY\nFROM: Autonomy Division\nTO: Corp Board\nRE: Meridian-88 — Expansion\n\nM-88 autonomy expanded: 3 districts.\nHuman oversight: fully symbolic.\nExpansion to all GLMZ: 6-month plan.\nLegal: non-existent — designed out.\n\n[SIG: 0xFF0000 // EXECUTIVE-ONLY]',
        'FROM: Corp Finance\nTO: Account Management\nRE: Slush Fund 0x3AF7 — Q4 Close\n\nTotal Q4 slush: Φ88M.\nSources: neural sales, diversion, land.\nLaundering: dist4/fab subs clean.\nDestroyreceipts — all of them. Now.\n\n>>> RELAY CORRUPTION — terminal <<<',
        'FROM: BCI Compliance (Covert)\nTO: Analytics Board\nRE: Passive Read — District 12\n\n4,412 operators read Q4 — no consent.\nData piped to NeuralState: confirmed.\nLogs purged 72h post-collection.\nCorp link: dark. Proceed Q1 expansion.\n\n[INTERCEPT CONFIDENCE: 0.91]',
        'TO: Corp Security, District 9\nFROM: Field Operations Director\nRE: Freelancer Safe-House — Strike\n\nSafe-house confirmed: 10 operators.\n3 have full Batch 7-11 records.\nStrike authorized — simultaneous.\n0400 UTC — dark-node coord only.\n\n[SIG: 0x9A4412 // EXECUTIVE]',
        'FROM: Population Analytics\nTO: Corp Board\nRE: GLMZ Dissent Metrics — Q4\n\nDissent index: 0.44 (down from 0.61).\nBCI suppression: 88% credit.\nFood throttle: 8% credit.\nPhase 5 on track — board approved.\n\n>>> CREST INTERNAL :: corp/audit <<<',
        'FROM: Legal Stratagem Group\nTO: Corp Counsel\nRE: Regulatory Inquiry #0441\n\nInquiry: BCI consent violations, Q1-Q4.\nStrategy: stonewall, delay, negotiate.\nMax settlement offer: Φ20M.\nIf criminal referral: see Protocol 9.\n\n[INTERCEPT: corp/legal-vault]',
        'CONFIDENTIAL — EYES ONLY\nFROM: Corp Board\nTO: All Division Heads\nRE: Op Silent Horizon — Complete\n\nSilent Horizon: complete across GLMZ.\n44,000 operators profiled + monetized.\nCompliance: 88% — above target.\nPhase 6 brief: next board meeting.\n\n[SIG: 0xFF0000 // BOARD ONLY]',
        'FROM: Intelligence Operations\nTO: Corp Security Central\nRE: External Investigation — Threat\n\nExternal investigator identified.\nHas partial Op Silent Horizon records.\nLevel 3 authorized — contain quietly.\nRoute dark-node — no enforcement log.\n\n>>> DARK NODE :: corp/sec <<<',
        'FROM: BCI Product (Covert)\nTO: Corp Revenue Board\nRE: Operator License Diversion\n\n800 BCI licenses resold at 350%.\nBuyers: 3 unlicensed gray-market ops.\nRevenue: Φ2.8M to account 0x3AF7.\nCrest audit: preempted — logs clean.\n\n[RELAY: sec/vault-12]',
        'TO: Enforcement Central\nFROM: Intelligence Division\nRE: Op Blackout — District 7\n\nFreelancer surge confirmed — cover.\nActual target: cell leader 0x3301.\nCollateral: acceptable up to 50%.\nClose window before 0600. Media: dark.\n\n[SIG: 0xBE0441 // SILENT]',
        'FROM: Corp Medical Division\nTO: BCI Trial Board\nRE: Adverse Outcomes — Cohort 10\n\n44 adverse events in Cohort 10.\n8 classified critical — 3 fatal.\nAll logged as pre-existing conditions.\nProceed Cohort 11 — board approved.\n\n>>> RELAY CORRUPTION — partial <<<',
        'FROM: Territory Acquisition\nTO: Legal + Field Security\nRE: District 9 Hostile Buyout\n\n6 orgs remaining — all refusing.\nOffer window: 7 days.\nDay 8: enforce using Protocol 7.\nExpected redundancies: 1,200-1,800.\n\n[INTERCEPT CONFIDENCE: 0.93]',
        'PRIORITY ALPHA — BOARD ONLY\nFROM: Autonomy Division\nTO: Corp Board\nRE: Meridian-88 — Full Deployment\n\nM-88 now operates all GLMZ sectors.\nHuman oversight: none remaining.\nDeactivation: physically impossible.\nThis is the last board brief on M-88.\n\n[SIG: 0xFF0000 // FINAL TRANSMISSION]',
        'FROM: Corp Finance (Covert)\nTO: NeuralState Consortium\nRE: Q4 Final Settlement — Φ88M\n\nFinal Q4 neural data settlement: Φ88M.\nDelivered: 440,000 unique profiles.\nAnonymization: re-linkable — noted.\nTransfer via dark-channel — confirmed.\n\n>>> RELAY: corp/broker :: VAULT <<<',
        'FROM: Legal Stratagem Group\nTO: Corp Board\nRE: Existential Exposure — Q4\n\nIf Silent Horizon leaks: all leadership.\nContingency: sacrifice 3 mid-tier execs.\nCover story: rogue division. Prepared.\nBoard members: immunity pre-arranged.\n\n[SIG: 0xDEAD4412 // BOARD-ONLY]',
        'FROM: Enforcement Central\nTO: ALL DISTRICT COMMANDS\nRE: Op Dark Census — Final Phase\n\nFinal phase: all unregistered — detain.\nBCI registration: mandatory by 0600.\nNon-compliance: permanent detention.\nMedia blackout: indefinite. No record.\n\n>>> RELAY CORRUPTION — CRITICAL <<<',
        'FROM: Intelligence Operations\nTO: Corp Board\nRE: Outside Press — Containment\n\n3 journalists, 2 external orgs.\nAll have Silent Horizon fragments.\nLevel 4 authorized — all targets.\n0300 UTC simultaneous. Dark only.\n\n[SIG: 0xFF0000 // EXECUTE]',
        'TO: Corp Board — Eyes Only\nFROM: Population Analytics Lead\nRE: GLMZ Compliance — Final Report\n\nCompliance: 91% — above all targets.\nDissent index: 0.31 — historic low.\nMortality from ops: 88 — within budget.\nBoard consensus: proceed Phase 7.\n\n[SIG: 0xDEAD9441 // FINAL]',
    ];

    // ── Warning popup data ──────────────────────────────────────────────────
    var WARN_TITLES = [
        'WARN — latency spike',     'WARN — retry limit',       'WARN — signal degraded',
        'WARN — disk 85% full',     'WARN — cert expires 7d',   'WARN — audit gap',
        'WARN — bci drift',         'WARN — temp elevated',     'WARN — corp deviation',
        'WARN — foreign pattern',   'WARN — rate limit near',   'WARN — replica lag',
        'WARN — operator anomaly',  'WARN — entropy low',       'WARN — auth failures',
        'WARN — memory pressure',   'WARN — port saturation',   'WARN — beacon change',
        'WARN — key rotation due',  'WARN — zone access freq',  'WARN — crest wear score',
        'WARN — hkb fluid low',     'WARN — net tunnel flap',   'WARN — db stats stale',
        'NOTICE — process flagged', 'NOTICE — unusual traffic', 'NOTICE — policy deviation',
        'NOTICE — shadow process',  'NOTICE — off-hours access','NOTICE — location mismatch',
        'NOTICE — glmz gate usage', 'NOTICE — schedule deviate',
        // 100 new entries
        'WARN — bci ring4 noise',      'WARN — bci ring5 noise',      'WARN — bci epoch slip',
        'WARN — bci band power low',   'WARN — bci snr 14dB',         'WARN — bci event overflow',
        'WARN — bci session age 8h',   'WARN — bci cal drift 0.9%',   'WARN — bci key age 90d',
        'WARN — bci batt 18%',         'WARN — rbs rotation slow',    'WARN — rbs detent wear',
        'WARN — rbs feed pressure low','WARN — rbs thermal 49C',      'WARN — rbs round count low',
        'WARN — rbs bolt tension',     'WARN — rbs primer misfire',   'WARN — rbs selector slop',
        'WARN — rbs ejector wear',     'WARN — rbs extractor weak',   'WARN — hkb psi drop 15%',
        'WARN — hkb spring 75% spec',  'WARN — hkb fluid 25% remain','WARN — hkb thermal 50C',
        'WARN — hkb wear score 0.68',  'WARN — hkb cycle 9,500',      'WARN — hkb seal leak',
        'WARN — bim conf below 0.75',  'WARN — bim retrain overdue',  'WARN — bim model age 30d',
        'WARN — bim adc ring2 drift',  'WARN — bim feat norm fail',   'WARN — bim label shift',
        'WARN — db compaction behind', 'WARN — db cache evict spike', 'WARN — db txn queue 80%',
        'WARN — db index fragmented',  'WARN — db page dirty 512',    'WARN — db wal lag 30s',
        'WARN — db stats 300s stale',  'WARN — db bloom fp rate up',  'WARN — db mvcc horizon',
        'WARN — net tunnel flap 4x',   'WARN — net jitter 12ms',      'WARN — net loss 0.08%',
        'WARN — net latency 90ms',     'WARN — net tcp retransmit 3%','WARN — net mtu mismatch',
        'WARN — net arp poison detect','WARN — net rst inject seen',   'WARN — net beacon shift 20kHz',
        'WARN — net quic stream stall','WARN — net bgp withdraw recv', 'WARN — net dns ttl expired',
        'WARN — sec ids alert L3',     'WARN — sec scan anomaly',     'WARN — sec cert 14d expiry',
        'WARN — sec token 6h remain',  'WARN — sec key 60d old',      'WARN — sec crl stale 2h',
        'WARN — sec firewall spike',   'WARN — sec acl shadow rule',  'WARN — sec mfa bypass attempt',
        'WARN — sec rng entropy 48b',  'WARN — sec hsm latency 80ms', 'WARN — sec vault shard 1',
        'WARN — glmz cam d12 offline', 'WARN — glmz sensor fault d9', 'WARN — glmz permit 3d expiry',
        'WARN — glmz gate use 60x',    'WARN — glmz dark node 10.44', 'WARN — glmz ntp drift 40ms',
        'WARN — glmz power unstable',  'WARN — glmz corridor sealed', 'WARN — glmz mesh gap d7',
        'WARN — glmz rf tag anomaly',  'WARN — glmz bio scan slow',   'WARN — glmz audio flag d4',
        'WARN — corp enclave lag',     'WARN — corp mirror gap 12s',  'WARN — corp audit 8 missing',
        'WARN — corp cred 7d expiry',  'WARN — corp license 5% left', 'WARN — corp policy v5 delta',
        'WARN — corp edr detection',   'WARN — corp dlp volume high', 'WARN — corp siem L3 event',
        'WARN — corp quota 88%',       'WARN — corp patch pending 7', 'WARN — corp territorial query',
        'NOTICE — bci foreign pattern','NOTICE — rbs manual override','NOTICE — hkb service window',
        'NOTICE — bim model version',  'NOTICE — corp key cycle due', 'NOTICE — sec vault shard 2',
        'NOTICE — net dark node seen',  'NOTICE — glmz district merge','NOTICE — new operator 0x8001',
        'NOTICE — enclave query spike', 'NOTICE — db graph 10k nodes', 'NOTICE — corp enclave join',
    ];

    var WARN_MSGS = [
        'Operator 0x3301 accessed zone B\n7 events in 24h — baseline is 2',
        'BCI latency 18ms — threshold 10ms\nClassifier confidence 0.71 — watch',
        'Cert glmz.relay.internal: 7d expiry\nRenewal scheduled — no action yet',
        'Disk /var/bci at 85% capacity\nSchedule cleanup — 2h to critical',
        'Replication lag 4.4s — watch\nPrimary write rate elevated 3×',
        'Entropy pool: 48 bits — low\nReseeding from RDRAND — ok for now',
        'HKB wear_score=0.72 — service soon\n88 cycles to recommended interval',
        'BCI drift: 0.8μV/min on ring2\nBelow alarm threshold — log started',
        'Operator 0x7712 off-hours access\n03:22 district4 — within policy',
        'Net beacon 0xBE04 freq change\n+12kHz shift — interference or move?',
        'Corp audit: 4 events out-of-order\nTimestamps within 200ms — clock skew?',
        'Auth failures uid=3301: 3 in 1h\nAbove baseline 1 — no lockout yet',
        'Memory RSS 1.4G — 73% of limit\nGrowth 44MB/h — check for leak',
        'glmz gate 12 use 44× today\nBaseline 12× — freelancer surge?',
        'Shadow process pid=31337 found\nNot in manifest — investigating',
        'Location mismatch: 0x4492\nLast seen d7, login from d12',
        'Corp policy deviation: uid=0x3301\n3 zone-B accesses without prior auth',
        'Foreign key pattern in BCI stream\nClassifier flagging unknown operator',
        'Key rotation 12d overdue: corp-vault\nSchedule window before expiry',
        'RBS thermal 47C — within spec\nElev. above baseline 38C — log',
        'HKB hydraulic: fluid 20% remaining\nRefill due before next session',
        'Net tunnel wg to dist4 flapped 3×\nStability: 94% uptime — watch',
        'DB stats 180s stale: bci_events\nPlanner using old data — ANALYZE',
        'Cert corp-enclave depth=1: 14d\nAuto-renew queued — monitor',
        'BCI ring3 impedance creeping up\n18kΩ — trend toward 22kΩ limit',
        // 30 more
        'RBS disc wear-score 0.61 — service at 0.80\n14,200 rotations logged this quarter',
        'BCI ring2 noise floor elevated\nBaseline 0.8μV → now 1.4μV — possible impedance',
        'HKB spring tension 87% spec\nFall below 80% triggers manual inspect',
        'Enclave attestation skew 3.2s\nClock drift — resync before next audit',
        'GLMZ node dist12 latency 88ms\nBaseline 22ms — check relay path',
        'Corp sync drift: 9 events behind\nPrimary load elevated — catch-up queued',
        'Auth token expiry in 4h: uid=0x4492\nRenewal queued — no action needed yet',
        'Relay packet loss 0.8%: dist9→dist12\nAbove baseline 0.1% — path degraded',
        'DB write amplification 4.8×\nLSM compaction behind — schedule window',
        'File descriptors 1800/2048\nApproaching limit — check for leaks',
        'CPU core3 throttling at 88°C\nCooling nominal — sustained load spike',
        'Swap pressure 14% — watch\nRSS trending up, GC may not keep pace',
        'TLS cert chain depth=4: corp-root\nUnusual — verify issuer hierarchy',
        'BCI motor cortex drift 0.6%/hr\nBelow abort threshold — calibrate next session',
        'Dark-node latency 340ms: glmz/7\nNominal <80ms — relay path congested',
        'Audit log 44s behind real-time\nWriter blocked on disk flush — ok for now',
        'Snapshot 48h stale: bci_events\nScheduled refresh missed — retry queued',
        'License utilization 88%: bci_nodes\n12 seats remain — notify procurement',
        'RNG entropy pool 32 bits\nBelow 64-bit threshold — kernel reseed pending',
        'BCI power reserve 12%\nBattery degradation flag — replace before next op',
        'IPC queue depth 420: bci_daemon\nNominal 50 — consumer falling behind',
        'HKB cycle count 9,800 — service at 10k\nSchedule maintenance before next session',
        'Corp enforcement notice: zone-C access\nOperator 0x3301 flagged — within policy',
        'Corp data retention horizon breach\nEvents older than 90d present — archive',
        'Corp enclave query volume spike\n3× baseline in last hour — investigate',
        'Off-site login: uid=0x7712 dist4\nLast known location: dist9 — review',
        'BCI key age 180d: uid=0x3301\nRotation overdue — schedule in 14d window',
        'Rate limit 90%: relay forwarding\nCorp quota — request increase or throttle',
        'New district node dist2/fab-9 online\nNot in routing table — manual review',
        'Freelancer 0x4492 on watchlist match\nLow confidence — monitor, no action yet',
        // 100 new entries
        'BCI ring4 noise floor 1.8μV\nBaseline 0.8μV — check impedance ring4',
        'BCI ring5 noise floor 2.1μV\nAbove 2.0μV threshold — log started',
        'BCI epoch slip: epoch=9442 late 3ms\nSched jitter — no data loss',
        'BCI band power delta 12Hz low\n0.4× baseline — check electrode contact',
        'BCI SNR 14dB: ring2\nNominal 28dB — possible interference',
        'BCI event overflow: drop 3 frames\n120Hz rate exceeded — throttle',
        'BCI session age 8h: operator 0x3301\nPolicy max 6h — flag for review',
        'BCI calibration drift 0.9%/hr\nBelow 1% abort threshold — log',
        'BCI neural key age 90d: uid=0x7712\nRotation due — schedule in 7d',
        'BCI battery 18%: operator 0x4492\nEst 1.2h remain — warn user',
        'RBS rotation 52ms — nominal 38ms\nMotor load elevated — watch',
        'RBS detent wear 62% — service at 80%\n8,100 rotations this quarter',
        'RBS feed pressure 85% spec\nChannel alpha — nominal range 90-110%',
        'RBS thermal 49°C — nominal 38°C\nElevated load — cooling check',
        'RBS round count: alpha 2, beta 0\nBeta channel empty — resupply',
        'RBS bolt tension 88% spec\nFall below 80% requires inspect',
        'RBS primer anomaly: 1 ignition delay\nWithin tolerance — log started',
        'RBS selector slop 0.3mm\nSpec 0.1mm — worn detent cam',
        'RBS ejector spring 77% spec\nMonitor — service at 70%',
        'RBS extractor grip 84% spec\nMonitor — service at 75%',
        'HKB pressure drop 15% from baseline\nCheck seals — refill may be needed',
        'HKB spring tension 75% spec\nService threshold 70% — watch',
        'HKB fluid 25% remaining\nEst 400 cycles to refill — schedule',
        'HKB thermal 50°C — nominal 42°C\nElevated sustained fire — cool down',
        'HKB wear score 0.68 — service at 0.80\n2,100 cycles to threshold',
        'HKB cycle count 9,500 — service at 10k\n500 cycles remain — schedule now',
        'HKB seal leak detected: 0.2 mL/hr\nBelow critical — monitor',
        'BIM classifier confidence 0.72\nBelow 0.80 threshold — next session cal',
        'BIM model age 30d: uid=0x3301\nRetraining recommended — schedule',
        'BIM ADC ring2 drift 0.4%/hr\nCalibrate at next session',
        'DB compaction 18h behind schedule\nWrite amplification 5.2× — queue window',
        'DB cache evict spikes: 200/min\nWorking set > cache — resize or tune',
        'DB txn queue at 80% depth\nHighest in 48h — monitor for deadlock',
        'DB index fragmented: bci_events\nFrag 34% — rebuild in off-peak window',
        'DB dirty pages 512 — flush behind\nWrite pressure elevated — check IO',
        'DB WAL lag 30s: primary overloaded\nCatch-up queued — monitor lag',
        'Net tunnel flap 4×: dist9→corp\nUptime 88% past hour — route check',
        'Net jitter 12ms: corp-relay path\nNominal 1ms — congestion or misdrop',
        'Net packet loss 0.08%: dist7→dist12\nAbove baseline 0.01% — path degraded',
        'Net TCP retransmit 3%: relay path\nAbove 1% threshold — congestion',
        'Net MTU mismatch: corp-relay 1480\nGlobal MTU 1500 — fragmentation',
        'Net ARP poisoning signal detected\nSource 10.44.7.44 — investigating',
        'Net RST injection: 3 in 1h\nPossible MITM — audit relay path',
        'Net beacon frequency shift +20kHz\nOperator 0xBE04 — move or interference?',
        'Net QUIC stream stall: relay:9000\nCongestion window 0 — backoff active',
        'Net BGP withdraw received: dist4\nRoute 10.44.4.0/24 pulled — failover',
        'Net DNS TTL expired: glmz.relay\nRe-resolving — brief 40ms outage',
        'SEC IDS alert L3: uid=0x4492\n3 anomalous auth events in 30min',
        'SEC scan anomaly: pid=31338\nUnexpected network probe — quarantine?',
        'SEC cert expires 14d: corp-relay\nAuto-renew queued — verify CA access',
        'SEC token 6h remain: uid=0x3301\nAuto-refresh should trigger at 4h',
        'SEC key age 60d: net-relay sign\nRotation due — schedule in 7d window',
        'SEC CRL stale 2h: corp-issuer\nFetch failing — check revocation server',
        'SEC firewall rule hits spike: 3×\nPort 9000 inbound — enforce or deny',
        'SEC ACL shadow rule detected\nRule 441 overlaps rule 88 — review',
        'SEC MFA bypass attempt: uid=0x1187\nFailed — account flagged',
        'SEC RNG entropy pool 48 bits\nBelow 64-bit threshold — kernel reseed',
        'SEC HSM latency 80ms\nNominal 4ms — check HSM load or link',
        'SEC vault shard 1 verify slow\nHMAC calc 220ms — nominal 4ms',
        'GLMZ cam dist12 offline: sector 12B\nFeed dark 22 min — check node',
        'GLMZ sensor fault district9: temp\nReading 999°C — sensor failure',
        'GLMZ permit expires 3d: 0x4492\nRenewal requires in-person corp ID',
        'GLMZ gate 7 use 60× today\nBaseline 18× — surge or evasion route?',
        'GLMZ dark node 10.44.7.44 seen\nUnregistered — corp enforcement flagged',
        'GLMZ NTP drift +40ms\nClock skew — audit logs may mis-sequence',
        'GLMZ power grid unstable: sector 4C\n3 sags in 1h — check UPS',
        'GLMZ corridor 9D sealed: access cut\nCorp enforcement order — reroute',
        'GLMZ mesh route gap: dist7→dist4\nHop d9 offline — fallback active',
        'GLMZ RF tag anomaly: 0x0441\nRegistered tag but wrong location',
        'GLMZ bio scan slow: 8s avg\nNominal 1s — iris scanner fault',
        'GLMZ audio anomaly: district4 node\nUnrecognized speech pattern flagged',
        'Corp enclave sync lag 18s\nPrimary overloaded — catch-up queued',
        'Corp mirror gap: 12s events missing\nTimestamp 03:44 — possible drop',
        'Corp audit 8 events out of order\nClock skew 400ms — within tolerance',
        'Corp credentials expire 7d: uid=0x8001\nAutomatic renewal in 5d',
        'Corp license 95% utilized: bci_nodes\n5 seats remain — request increase',
        'Corp policy v5 delta: 3 new rules\nRules 441-443 — review before sync',
        'Corp EDR detection: pid=3301\nHeuristic match — manual review',
        'Corp DLP volume spike: uid=0x3301\n3× baseline outbound — inspect',
        'Corp SIEM L3 event: relay auth fail\n4 events correlated — low confidence',
        'Corp quota 88%: relay forwarding\nΦ rate limit — request increase',
        'Corp patch 7 pending: bci_daemon\nHighest CVSSv3: 7.2 — schedule window',
        'Corp territorial query: dist2 assets\nUnusual volume — legal review flag',
        'BCI foreign pattern in stream\nClassifier 0.61 confidence — log',
        'RBS manual override lever engaged\nBCI mode suspended — log started',
        'HKB service window recommended\nSchedule before next field session',
        'BIM model version mismatch: v3 vs v4\nRollback available — review',
        'Corp key cycle due: enclave-sign\nSchedule in next 48h window',
        'SEC vault shard 2 HMAC warn\nShard 2/3 slightly slow — watch',
        'Net dark node traffic seen\nNon-corp destination — flag',
        'GLMZ district merge topology change\nRouting table updated — verify',
        'New operator 0x8001 registered\nProfile not in baseline — monitor',
        'Corp enclave query spike 4×\nLast 30min — investigate caller',
        'DB graph exceeded 10k nodes\nIndex rebuild recommended',
        'Corp enclave join: new node dist2/fab-9\nNot in approved list — verify',
        'BCI session log write behind 3s\nWriter queue 80% — check IO',
        'GLMZ enforcement beacon change\nDistrict 9 active — freelancers dispersing',
        'Corp sync conflict: 2 events\nAuto-resolve queued — verify after',
        'Net relay cert pinning mismatch\nCert changed unexpectedly — audit',
        'BCI model weight hash differs\nPossible tamper — rollback staged',
        'RBS rotation timeout recovered\nSoft retry succeeded — log #3',
    ];

    // ── Glyph character pool ────────────────────────────────────────────────
    var GLYPH_CHARS =
        '░▒▓█▌▐▀▄■□▪▫◆◇○●◉◎⊗⊕⊙∅' +
        '∞≠≈∫∂∆Ωπμλφψξζ' +
        '⌂⌀⌘⌬⌭⌫' +
        '✦✧✩✫✭✯✱✲✳✴✵✶✷✸✹✺✻✼' +
        '⬡⬢⬠⬟⬜⬝' +
        '⠿⠻⠷⠾⠽⠯⠫⠳' +
        '₿€¥₩₹Φ₽₼₺₴₦' +
        'アイウエオカキクケコサシスセソタチツテトナニヌネノ' +
        'ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρστυφχψω' +
        '龍炎風水土金木火' +
        '가나다라마바사아자차카타파하갈람밤삼잠참캄탐팜함';

    function eraseMemo(el) {
        var BLOCK = '█▓▒░▪■▫';
        var chars = el.textContent.split('');
        var total = chars.length;
        var phase = 0;
        var count = 0;
        var timer = setInterval(function () {
            var n = rand(4, 10);
            for (var i = 0; i < n; i++) {
                var idx = rand(0, total - 1);
                if (chars[idx] === '\n') continue;
                if (phase === 0) {
                    chars[idx] = BLOCK[rand(0, BLOCK.length - 1)];
                } else {
                    chars[idx] = ' ';
                }
            }
            el.textContent = chars.join('');
            count += n;
            if (phase === 0 && count >= total * 1.6) { phase = 1; count = 0; }
            if (phase === 1 && count >= total * 1.6) {
                clearInterval(timer);
                if (el.parentNode) el.parentNode.removeChild(el);
            }
        }, 22);
    }

    function spawnMemo() {
        var host = getHost();
        if (!host) return;
        var el = document.createElement('div');
        el.className = 'cbg-memo';
        var mp = bestPos(host, 240, 110, -5, 78, 4, 88);
        el.style.left = mp[0] + '%';
        el.style.top  = mp[1] + '%';
        el.textContent = pick(MEMOS);
        host.appendChild(el);
        setTimeout(function () { eraseMemo(el); }, rand(3000, 6000));
    }

    // ── Scrolling texture layer ─────────────────────────────────────────────

    var TEX_SRCS = [
        '/api/media/circuitboard.00.png',
        '/api/media/circuitboard.01.png',
        '/api/media/circuitboard.02.png',
    ];
    var texLayers = null;
    var texRaf    = null;
    var texTimer  = null;

    function initTextures() {
        var host = getHost();
        if (!host) return;

        var canvas = host.querySelector('.cbg-tex');
        if (!canvas) {
            canvas = document.createElement('canvas');
            canvas.className = 'cbg-tex';
            canvas.style.cssText = 'position:absolute;inset:0;pointer-events:none;z-index:0;';
            host.insertBefore(canvas, host.firstChild);
        }
        canvas.width  = window.innerWidth;
        canvas.height = window.innerHeight;
        var ctx = canvas.getContext('2d');

        if (!texLayers) {
            texLayers = TEX_SRCS.map(function (src, i) {
                var img = new Image();
                img.src = src;
                var angle = (Math.PI * 2 / 3) * i + 0.4;
                var speed = 0.10 + i * 0.03;
                return {
                    img:     img,
                    dx:      i * 80,
                    dy:      i * 55,
                    vx:      Math.cos(angle) * speed,
                    vy:      Math.sin(angle) * speed,
                    opacity: [0.015, 0.010, 0.008][i],
                };
            });
        }

        function schedDir() {
            texTimer = setTimeout(function () {
                if (!texLayers) return;
                var t = texLayers[Math.floor(Math.random() * texLayers.length)];
                var a = Math.random() * Math.PI * 2;
                var s = 0.07 + Math.random() * 0.13;
                t.vx = Math.cos(a) * s;
                t.vy = Math.sin(a) * s;
                schedDir();
            }, 3000 + Math.random() * 4000);
        }
        if (!texTimer) schedDir();

        if (texRaf) cancelAnimationFrame(texRaf);
        var w = canvas.width, h = canvas.height;

        function frame() {
            ctx.clearRect(0, 0, w, h);
            texLayers.forEach(function (t) {
                if (!t.img.complete || !t.img.naturalWidth) return;
                var iw = t.img.width, ih = t.img.height;
                t.dx = ((t.dx + t.vx) % iw + iw) % iw;
                t.dy = ((t.dy + t.vy) % ih + ih) % ih;
                ctx.globalAlpha = t.opacity;
                for (var x = -t.dx; x < w + iw; x += iw) {
                    for (var y = -t.dy; y < h + ih; y += ih) {
                        ctx.drawImage(t.img, x, y);
                    }
                }
            });
            ctx.globalAlpha = 1;
            texRaf = requestAnimationFrame(frame);
        }
        frame();
    }

    // ── Tick loop ───────────────────────────────────────────────────────────

    function spawnCascadeError() {
        var n = rand(2, 4);
        var x = rand(10, 30), y = rand(8, 22);
        var stepX = rand(9, 14), stepY = rand(8, 13);
        for (var i = 0; i < n; i++) {
            (function (delay, px, py) {
                setTimeout(function () { spawnError(px, py); }, delay);
            })(i * rand(120, 320), x + i * stepX, y + i * stepY);
        }
    }

    // ── Floating artifact clusters ───────────────────────────────────────────

    function spawnArtifact() {
        var host = getHost();
        if (!host) return;

        var palettes = ['cbg-artifact--red', 'cbg-artifact--white', 'cbg-artifact--blue', 'cbg-artifact--amber'];
        var el = document.createElement('div');
        el.className = 'cbg-artifact ' + pick(palettes);
        el.style.left = rand(-2, 90) + '%';
        el.style.top  = rand(-2, 90) + '%';
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';

        var n = rand(14, 28);      // fewer chars — less center accumulation
        var blobR = rand(40, 80);  // more spread — less density pileup
        // One drift direction per cluster — glyphs share a destination with individual spread
        var clusterAngle = Math.random() * Math.PI * 2;

        for (var i = 0; i < n; i++) {
            var span = document.createElement('span');
            span.className = 'cbg-artifact-char';
            var angle = Math.random() * Math.PI * 2;
            // Uniform random r (not sqrt) gives even area distribution — no dense centre
            var r     = Math.random() * blobR;
            span.style.left     = Math.round(r * Math.cos(angle)) + 'px';
            span.style.top      = Math.round(r * Math.sin(angle)) + 'px';
            span.style.fontSize = rand(9, 20) + 'px';
            // Tiny base blur — chars start crisp; the drift keyframe multiplies this as they travel
            span.style.setProperty('--abl', (0.3 + Math.random() * 0.6).toFixed(1) + 'px');
            // Target: cluster direction ± small individual spread (~±25°)
            var glyphAngle = clusterAngle + (Math.random() - 0.5) * 0.88;
            var glyphDist  = rand(50, 130);
            span.style.setProperty('--adx1', Math.round(Math.cos(glyphAngle) * glyphDist) + 'px');
            span.style.setProperty('--ady1', Math.round(Math.sin(glyphAngle) * glyphDist) + 'px');
            // Perpendicular sway — direction: (-sinθ, cosθ); sign randomised per glyph
            // so ~half the cluster fans left-first, half right-first → gather & spread rhythm
            var swayAmp  = rand(10, 28);
            var swaySign = Math.random() < 0.5 ? 1 : -1;
            span.style.setProperty('--sx', Math.round(-Math.sin(glyphAngle) * swayAmp * swaySign) + 'px');
            span.style.setProperty('--sy', Math.round( Math.cos(glyphAngle) * swayAmp * swaySign) + 'px');
            // Varied duration + stagger so glyphs don't all vanish simultaneously
            var driftDur   = (1.8 + Math.random() * 2.2).toFixed(2); // 1.8–4s — faster swim
            var driftDelay = (Math.random() * 1.8).toFixed(2);        // 0–1.8s stagger
            // ease-in: lazy start, accelerates — feels purposeful, not mechanical
            span.style.animation = 'cbg-art-flicker 0.7s steps(1) infinite, cbg-art-drift ' + driftDur + 's ease-in ' + driftDelay + 's forwards';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
        }

        host.appendChild(el);

        // Fade in after paint
        requestAnimationFrame(function () {
            requestAnimationFrame(function () { el.style.opacity = '1'; });
        });

        // Remove container after all glyphs have dissolved (max drift + delay + buffer)
        var ttl = rand(6000, 8000);
        setTimeout(function () {
            if (el.parentNode) el.parentNode.removeChild(el);
        }, ttl);
    }

    // ── Spawn rate constants — edit here to tune both hosts identically ────────
    var RATE_ERROR    = 0.01;  // fatal error popups
    var RATE_WARN     = 0.01;  // warning popups
    var RATE_MEMO     = 0.04;  // corporate memo intercepts
    var RATE_GEO      = 0.10;  // geometric schematic windows
    var RATE_CASCADE  = 0.03;  // cascading console window burst
    var RATE_ARTIFACT = 0.12;  // floating glyph artifact clusters
    var RATE_FRAG     = 0.44;  // floating code fragments
    // RATE_WIN = remainder (~0.23) — console windows (black / blue / amber)

    function tickDelay() {
        var area = window.innerWidth * window.innerHeight;
        var scale = Math.max(0.35, Math.min(2.5, (1920 * 1080) / area));
        return rand(500, 1800) * scale;
    }

    function tick() {
        if (!getHost()) { tickTimer = null; return; }
        var r = Math.random(), t = 0;
        if      (r < (t += RATE_ERROR))    spawnError();
        else if (r < (t += RATE_WARN))     spawnWarning();
        else if (r < (t += RATE_MEMO))     spawnMemo();
        else if (r < (t += RATE_GEO))      spawnGeoWindow();
        else if (r < (t += RATE_CASCADE))  spawnCascade();
        else if (r < (t += RATE_ARTIFACT)) spawnArtifact();
        else if (r < (t += RATE_FRAG))     spawnFrag();
        else                                spawnWindow();
        tickTimer = setTimeout(tick, tickDelay());
    }

    // ── Quake lightstyle scan lines ─────────────────────────────────────────
    // Each char 'a'–'z': brightness = (charCode-97)/12.  'm'=1.0 (normal).
    // Two layers run at slightly different frame rates — they never phase-lock.
    // Reference styles from id Software Quake engine source (1996):
    //   style  1: "mmnmmommommnonmmonqnmmo"         (flicker)
    //   style 10: "mmamammmmammamamaaamammma"        (fluorescent flicker)

    var SL_FINE_STYLE   = 'mmnmmommommnonmmonqnmmo';
    var SL_COARSE_STYLE = 'mmamammmmammamamaaamammma';
    var SL_FINE_MAX     = 0.30;
    var SL_COARSE_MAX   = 0.22;

    var slFineIdx = 0, slCoarseIdx = 0;
    var slFineTimer = null, slCoarseTimer = null;

    function slStep(style, idx) {
        return (style.charCodeAt(idx) - 97) / 12;
    }

    function startScanLines() {
        if (slFineTimer)   { clearInterval(slFineTimer);   slFineTimer = null; }
        if (slCoarseTimer) { clearInterval(slCoarseTimer); slCoarseTimer = null; }

        slFineTimer = setInterval(function () {
            var el = document.querySelector('.cbg-sl-fine');
            if (!el) { clearInterval(slFineTimer); slFineTimer = null; return; }
            el.style.opacity = slStep(SL_FINE_STYLE, slFineIdx) * SL_FINE_MAX;
            slFineIdx = (slFineIdx + 1) % SL_FINE_STYLE.length;
        }, 100);

        slCoarseTimer = setInterval(function () {
            var el = document.querySelector('.cbg-sl-coarse');
            if (!el) { clearInterval(slCoarseTimer); slCoarseTimer = null; return; }
            el.style.opacity = slStep(SL_COARSE_STYLE, slCoarseIdx) * SL_COARSE_MAX;
            slCoarseIdx = (slCoarseIdx + 1) % SL_COARSE_STYLE.length;
        }, 107);
    }

    function stopScanLines() {
        if (slFineTimer)   { clearInterval(slFineTimer);   slFineTimer = null; }
        if (slCoarseTimer) { clearInterval(slCoarseTimer); slCoarseTimer = null; }
        var f = document.querySelector('.cbg-sl-fine');
        var c = document.querySelector('.cbg-sl-coarse');
        if (f) f.style.opacity = '0';
        if (c) c.style.opacity = '0';
    }

    function start() {
        var host = getHost();
        if (!host) return;
        initTextures();           // always reinit textures on this host
        if (tickTimer) return;    // tick loop already running
        startScanLines();
        tickTimer = setTimeout(tick, rand(500, 1500));
    }

    // Initial start
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }

    // Restart whenever .console-bg-host is added back to the DOM (Blazor navigation)
    new MutationObserver(function (mutations) {
        for (var i = 0; i < mutations.length; i++) {
            var nodes = mutations[i].addedNodes;
            for (var j = 0; j < nodes.length; j++) {
                var n = nodes[j];
                if (n.nodeType !== 1) continue;
                if ((n.classList && n.classList.contains('console-bg-host')) ||
                    (n.querySelector && n.querySelector('.console-bg-host'))) {
                    start();
                    return;
                }
            }
        }
    }).observe(document.body, { childList: true, subtree: true });

    return { start: start };
})();
