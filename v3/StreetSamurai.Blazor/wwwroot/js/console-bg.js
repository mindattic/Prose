window.consoleBg = (function () {
    'use strict';

    // ════════════════════════════════════════════════════════════════════════
    // EFFECT NAME REGISTRY
    // ════════════════════════════════════════════════════════════════════════
    // All console-bg effects have proper names. Refer to them by these.
    //
    // TOP-LEVEL EFFECTS (each has FX_*/RATE_* in the tick dispatcher):
    //   CRASH      — fatal-error popup           (spawnError)
    //   TREMOR     — warning popup               (spawnWarning)
    //   LEAK       — leaked corp memo            (spawnMemo)
    //   SCHEMATIC  — geometric schematic window  (spawnGeoWindow)
    //   CASCADE    — burst of console windows    (spawnCascade)
    //   ARTIFACT   — floating glyph cluster      (spawnArtifact)
    //   FRAGMENT   — floating code fragment      (spawnFrag)
    //   TRACE      — Tron-cycle network wire     (spawnNetConnect)
    //   PULSAR     — Morse-code glowing dot      (spawnMorseDot)
    //   HEIST      — folder-rip file extraction  (spawnFolderRip)
    //   PREDATOR   — artifact-hunting swarm      (spawnArtifactPredator)
    //   TERMINAL   — generic console window      (spawnWindow)
    //
    // ARTIFACT BEHAVIOR VARIANTS (rolled per spawn — see ART_VARIANTS):
    //   SCATTER    — random blob, all glyphs drift one direction (original)
    //   LATTICE    — Fibonacci grid, whole lattice drifts with corner-wave delay
    //   ANCHOR     — stationary grid, glitches in place, leading edge emits feelers
    //   SLUG       — single grid crawls + per-cell undulation
    //   CENTIPEDE  — multi-segment chain, peristaltic wave + leader feelers
    //   PULSE      — concentric Fibonacci rings, lub-dub heartbeat radiating outward
    //   WANDERER   — small grid walks the screen, pauses to "look around"
    //   SPIDER     — 8-leg radial cluster; legs alternate-pulse in walking gait
    //   INCHWORM   — linear chain bunches and extends, translating per cycle
    //   HOPPER     — cluster crouches, launches in parabolic arc, lands, repeats
    //   JELLY      — concentric rings pulse outward/in; whole body drifts vertically
    //   SQUID      — pulse-jet propulsion (burst + glide), trailing tentacles sway
    //   BEETLE     — scurry-stop-scurry rhythm with shell shimmer and antennae
    //   TADPOLE    — small body + long sinusoidal whipping tail (tail propels motion)
    //   ANT        — zigzag wobble path with two antennae feelers at the front
    //   MOIRE      — two overlapping rotated grids; interference patterns shift
    //   VORTEX     — spiral arms rotate; glyphs drift along the inflow spiral
    //   EXPLOSION  — outward burst with deceleration, then float
    //   ORBIT      — multiple concentric rings rotate at golden-ratio speeds
    //   NETWORK    — nodes float on individual axes; visible packets travel between
    //   PHASEFIELD — wide grid; each cell phases brightness by (x+y+t) sine
    //   SHATTER    — single cluster fractures into N pieces that drift apart
    //   SPIROGRAM  — glyphs trail along an epitrochoid (Spirograph) path
    //   PULSARRING — ring with a brightness wave traveling around it
    //
    // PULSAR (Morse) MODE VARIANTS:
    //   BLINK      — classic on/off pulse mode (90% of spawns)
    //   SHIFT      — slides cardinal directions, color-swaps each symbol (10%)
    //
    // TRACE (network wire) SUB-BEHAVIORS:
    //   ARC        — sharp-turn spark burst at corners (~30% of corners)
    //   ACK        — three-blink success signal then synced fade-out
    //   SEVER      — direction-aligned CONNECTION-LOST message on failure
    //
    // HEIST (folder rip) PHASES:
    //   HIGHLIGHT  — cyan selection glow on adjacent run of files
    //   EXTRACT    — sleek slide-right exit with shimmer + fade (per-file stagger)
    //   DISSOLVE   — window fade-out tied to extract completion
    //
    // PREDATOR sub-behaviors:
    //   STALK      — off-screen swarm origin, homes on prey
    //   SCAN       — prey detection cone (max forward, min behind)
    //   FLEE       — prey panic-redirect of crawl vector away from swarm
    //   DEVOUR     — cell consume-and-convert (cell adopts wasp glyph then dissolves)
    //   DISPERSE   — wasps scatter and fade after kill
    // ════════════════════════════════════════════════════════════════════════

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
        'bci: motor_rx lat=2ms ok',
        'bci: cortex_tx pkt=1 sz=48',
        'bci: grip_cls intent=sweep',
        'bci: calibrate ring=0 ok',
        'bci: neural_key verify ok',
        'bci: bim_proc pid=9144 up',
        'bci: contact_ring 3 active',
        'bci: sweep_score=0.92 ok',
        'bci: precision_score=0.87',
        'bci: epoch 4412 complete',
        'bci: delta_band 13Hz ok',
        'bci: theta_band 7Hz ok',
        'bci: alpha suppress ok',
        'bci: beta_burst detected',
        'bci: gamma spike flt ok',
        'bci: baseline update ok',
        'bci: noise_floor 0.04 ok',
        'bci: ring0 impedance ok',
        'bci: ring1 impedance ok',
        'bci: ring2 impedance ok',
        'bci: ring3 impedance ok',
        'bci: ring4 contact lost',
        'bci: ring4 reacquire ok',
        'bci: calibrate epoch=88',
        'bci: key_derive pbkdf2 ok',
        'bci: hmac verify sig ok',
        'bci: neural_rx buf=2048',
        'bci: cortex_sync 4.1ms',
        'bci: intent=precision ok',
        'bci: intent=idle set',
        'bci: lockout clear ok',
        'bci: operator 0x3301 ok',
        'bci: operator 0x7712 ok',
        'bci: operator 0x4492 auth',
        'bci: handshake accepted',
        'bci: grip_cls conf=0.95',
        'bci: grip_cls conf=0.78',
        'bci: motor signal 48Hz',
        'bci: adc ring=0 val=2048',
        'bci: adc ring=1 val=2051',
        'bci: adc ring=2 val=2039',
        'bci: adc ring=3 val=2044',
        'bci: feature_vec norm ok',
        'bci: classifier v3 load',
        'bci: model_weights hash ok',
        'bci: poll_interval 20ms',
        'bci: slot_queue depth=3',
        'bci: slot_queue drain ok',
        'bci: buffer_flush ok',
        'rbs: disc rotate α→β ok',
        'rbs: disc rotate β→α ok',
        'rbs: port_sel alpha set',
        'rbs: port_sel beta set',
        'rbs: led red active',
        'rbs: led blue active',
        'rbs: led yellow blink',
        'rbs: led green steady',
        'rbs: pin extend ok',
        'rbs: pin retract ok',
        'rbs: slug_feed ch=alpha ok',
        'rbs: slug_feed ch=beta ok',
        'rbs: buck_feed ch=alpha ok',
        'rbs: disc pos=0 locked',
        'rbs: disc pos=1 locked',
        'rbs: rotation 180ms ok',
        'rbs: rotation timeout!',
        'rbs: rotation retry ok',
        'rbs: thermal 38C ok',
        'rbs: thermal 47C warn',
        'rbs: round_count=12',
        'rbs: round_count=7',
        'rbs: empty detect ch=a',
        'rbs: feed_pressure ok',
        'rbs: port_lock engage',
        'rbs: port_lock release',
        'rbs: crest_sig v2.3 ok',
        'rbs: cal_ref 0x3A ok',
        'rbs: bim_cmd recv ok',
        'rbs: override manual set',
        'rbs: override clear ok',
        'hkb: piston extend 3mm',
        'hkb: piston retract ok',
        'hkb: buffer impulse 4.1N',
        'hkb: buffer impulse 6.2N',
        'hkb: buffer impulse 2.8N',
        'hkb: correction +1.2ms',
        'hkb: correction -0.8ms',
        'hkb: recoil absorb ok',
        'hkb: hydraulic psi=220',
        'hkb: hydraulic psi=198',
        'hkb: hydraulic low warn',
        'hkb: fluid level ok',
        'hkb: fluid level low!',
        'hkb: spring tension ok',
        'hkb: cycle complete ok',
        'hkb: cycle_count=4412',
        'hkb: wear_score=0.12',
        'hkb: wear_score=0.87!',
        'hkb: service_due flag',
        'hkb: thermal 42C ok',
        'net.tx 10.44.7.3:8443 1200B',
        'net.tx 172.16.0.1:443 640B',
        'net.rx 10.44.7.3:8443 880B',
        'net.rx 172.16.44.12:5555 400B',
        'net.tx glmz-relay:9000 512B',
        'net.rx glmz-gate:4433 288B',
        'net.tx dist7.node:8080 960B',
        'net.rx dist4.hub:7070 720B',
        'net.tx dist12.relay:6060 384B',
        'net.rx dist9.node:5050 1024B',
        'net.tx enf-node-44:443 256B',
        'net.rx enf-node-44:443 128B',
        'net.tx dark-node:9999 192B',
        'net.tls handshake ok 1.3',
        'net.tls cert verify ok',
        'net.tls sni glmz.relay ok',
        'net.dns resolve relay ok',
        'net.dns resolve failed!',
        'net.dns ttl=300 cached',
        'net.gate check pass ok',
        'net.gate check fail 403',
        'net.gate retry ok',
        'net.proxy CONNECT ok',
        'net.proxy tunnel up',
        'net.proxy tunnel drop',
        'net.scan port 8443 open',
        'net.scan port 443 open',
        'net.scan port 9000 open',
        'net.beacon recv district7',
        'net.beacon recv district4',
        'net.beacon enf active!',
        'net.rst inject detect',
        'net.conn reset peer',
        'net.tcp retry x3 ok',
        'net.tcp backoff 200ms',
        'net.tcp window=65535',
        'net.udp drop rate=0.02',
        'net.route via 10.44.0.1',
        'net.route metric=100 ok',
        'net.route failover ok',
        'net.bridge dist4→dist7',
        'net.tunnel wg up ok',
        'net.tunnel wg drop',
        'net.tunnel wg rekey ok',
        'kern: alloc 8192 ok',
        'kern: alloc 16384 ok',
        'kern: alloc fail ENOMEM',
        'kern: free 0x7ffe3200 ok',
        'kern: mmap sz=8192 rw',
        'kern: mmap sz=65536 rx',
        'kern: munmap ok',
        'kern: fault #14 prot r',
        'kern: fault #14 prot w',
        'kern: fault handled ok',
        'kern: signal 11 pid=9144',
        'kern: signal 9 pid=3301',
        'kern: signal 15 ok',
        'kern: sched preempt ok',
        'kern: sched migrate cpu2',
        'kern: sched wakeup pid=8812',
        'kern: irq 44 handled ok',
        'kern: irq 12 deferred',
        'kern: irq storm cpu0!',
        'kern: vm swapout 4p',
        'kern: vm swapin 2p',
        'kern: vm oom cand pid=3301',
        'kern: vm reclaim 8M ok',
        'kern: rcu_sched stall warn',
        'kern: softirq NET_RX',
        'kern: softirq TASKLET',
        'kern: hrtimer fire ok',
        'kern: clocksource tsc ok',
        'kern: audit pid=8812 ok',
        'kern: audit write denied',
        'kern: ptrace attach ok',
        'kern: seccomp filter ok',
        'kern: seccomp deny open',
        'kern: cgroup mem limit hit',
        'kern: cgroup cpuset ok',
        'kern: namespaces pid ok',
        'kern: namespaces net ok',
        'kern: landlock rule ok',
        'kern: ebpf prog load ok',
        'kern: ebpf map update ok',
        'kern: ebpf verifier pass',
        'kern: kprobes attach ok',
        'kern: perf sample cpu0',
        'kern: numa node0 alloc',
        'kern: numa node1 alloc',
        'kern: thp collapse ok',
        'kern: huge page alloc ok',
        'sys: daemon bci_d up',
        'sys: daemon net_d up',
        'sys: daemon sec_d up',
        'sys: daemon db_d up',
        'sys: daemon jit_d up',
        'sys: fork pid=9144 ok',
        'sys: exec bci_daemon ok',
        'sys: exec netd ok',
        'sys: sync ok',
        'sys: fsync pid=8812 ok',
        'sys: watchdog pet ok',
        'sys: watchdog expire!',
        'sys: reboot pending',
        'sys: shutdown signal',
        'sys: service restart ok',
        'sys: service fail x3',
        'sys: socket create ok',
        'sys: socket bind ok',
        'sys: socket listen ok',
        'sys: accept conn ok',
        'sys: close fd=7 ok',
        'sys: read fd=3 512B ok',
        'sys: write fd=4 256B ok',
        'sys: ioctl 0x5401 ok',
        'sys: mlock 4096 ok',
        'sys: chroot ok',
        'sys: setuid 1001 ok',
        'sys: setgid 1001 ok',
        'sys: capget ok',
        'sys: capset drop ok',
        'sys: prctl PR_SET_NO_DUMPABLE',
        'sys: prctl PR_SET_PDEATHSIG',
        'db: edge_insert n=5 ok',
        'db: edge_insert n=12 ok',
        'db: edge_delete n=2 ok',
        'db: node_insert ok',
        'db: node_delete ok',
        'db: node_lookup ok',
        'db: index_build ok',
        'db: index_update ok',
        'db: txn begin ok',
        'db: txn commit ok',
        'db: txn rollback ok',
        'db: wal flush ok',
        'db: wal checkpoint ok',
        'db: wal rotate ok',
        'db: vacuum start ok',
        'db: vacuum complete ok',
        'db: snapshot create ok',
        'db: snapshot restore ok',
        'db: repl sync ok',
        'db: repl lag=12ms',
        'db: repl lag=450ms warn',
        'db: cache hit ratio=0.94',
        'db: cache evict n=8',
        'db: query 2.1ms ok',
        'db: query 44ms slow',
        'db: query plan scan',
        'db: query plan index',
        'db: page dirty=128',
        'db: page flush ok',
        'db: page alloc ok',
        'db: integrity check ok',
        'db: integrity fail!',
        'db: corruption detect!',
        'db: repair attempt ok',
        'db: lock acquire ok',
        'db: lock timeout!',
        'db: deadlock abort',
        'db: row count=44120',
        'db: row count=9871',
        'sec: audit event ok',
        'sec: audit write ok',
        'sec: audit flush ok',
        'sec: policy check ok',
        'sec: policy deny!',
        'sec: vault seal ok',
        'sec: vault unseal ok',
        'sec: vault read ok',
        'sec: vault write ok',
        'sec: cap check ok',
        'sec: cap deny!',
        'sec: cert verify ok',
        'sec: cert expire warn',
        'sec: cert revoke ok',
        'sec: token verify ok',
        'sec: token expire ok',
        'sec: token refresh ok',
        'sec: token invalid!',
        'sec: role check ok',
        'sec: role deny!',
        'sec: scan clean ok',
        'sec: scan threat!',
        'sec: ids alert L2',
        'sec: ids alert L4',
        'sec: ids clear ok',
        'sec: firewall allow ok',
        'sec: firewall deny!',
        'sec: firewall update ok',
        'sec: key rotate ok',
        'sec: key expire ok',
        'sec: key derive ok',
        'sec: key import ok',
        'sec: key delete ok',
        'sec: intrusion attempt!',
        'sec: intrusion block ok',
        'sec: escalation attempt!',
        'sec: escalation block ok',
        'sec: exfil detect!',
        'sec: exfil block ok',
        'sec: rootkit sig match!',
        'sec: rootkit quarantine',
        'crypt: aes256 enc ok',
        'crypt: aes256 dec ok',
        'crypt: chacha20 enc ok',
        'crypt: chacha20 dec ok',
        'crypt: hmac-sha256 ok',
        'crypt: hmac verify ok',
        'crypt: hmac mismatch!',
        'crypt: ecdh derive ok',
        'crypt: ecdh keygen ok',
        'crypt: rng seed ok',
        'crypt: rng reseed ok',
        'crypt: rng entropy ok',
        'crypt: rng low entropy!',
        'crypt: pbkdf2 ok 100k',
        'crypt: pbkdf2 fail!',
        'crypt: rsa sign ok',
        'crypt: rsa verify ok',
        'crypt: rsa fail!',
        'crypt: ed25519 sign ok',
        'crypt: ed25519 verify ok',
        'crypt: ed25519 fail!',
        'crypt: x25519 dh ok',
        'crypt: sha256 hash ok',
        'crypt: sha512 hash ok',
        'crypt: blake3 hash ok',
        'crypt: gcm tag ok',
        'crypt: gcm tag fail!',
        'crypt: nonce wrap warn!',
        'crypt: key_schedule ok',
        'crypt: iv generate ok',
        'glmz: gate 44 check ok',
        'glmz: gate 44 deny!',
        'glmz: gate 12 check ok',
        'glmz: gate 12 deny!',
        'glmz: gate 7 check ok',
        'glmz: gate 7 deny!',
        'glmz: gate 9 check ok',
        'glmz: dist4 node up',
        'glmz: dist7 node up',
        'glmz: dist9 node up',
        'glmz: dist12 node up',
        'glmz: dist4 node down!',
        'glmz: dist7 node down!',
        'glmz: enf beacon active',
        'glmz: enf beacon clear',
        'glmz: dark node 10.44.7.99',
        'glmz: dark node hidden',
        'glmz: relay up ok',
        'glmz: relay down!',
        'glmz: relay switch ok',
        'glmz: freelancer 0x3301',
        'glmz: freelancer 0x7712',
        'glmz: freelancer 0x4492',
        'glmz: freelancer 0x1187',
        'glmz: id suspend 0x3301!',
        'glmz: id clear 0x3301 ok',
        'glmz: lockdown district7!',
        'glmz: lockdown clear ok',
        'glmz: border cross ok',
        'glmz: border deny!',
        'glmz: topology update ok',
        'glmz: topology gap detect',
        'glmz: comms encrypted ok',
        'glmz: comms intercept!',
        'glmz: mesh route ok',
        'glmz: mesh route fail',
        'corp: mirror sync ok',
        'corp: mirror sync fail',
        'corp: enclave join ok',
        'corp: enclave leave ok',
        'corp: audit log write ok',
        'corp: audit log gap!',
        'corp: policy v4.2 load',
        'corp: policy deny uid=3301',
        'corp: key rotate ok',
        'corp: key rotate fail!',
        'corp: enforcer ping ok',
        'corp: enforcer alert!',
        'corp: monitor event ok',
        'corp: monitor alert!',
        'corp: sync delta ok',
        'corp: sync conflict!',
        'corp: enclave attest ok',
        'corp: enclave attest fail',
        'corp: license check ok',
        'corp: license revoke!',
        'corp: territorial ok',
        'corp: territorial dispute!',
        'mem: alloc 4096 ok',
        'mem: alloc 65536 ok',
        'mem: alloc fail ENOMEM',
        'mem: free 4096 ok',
        'mem: mmap anon ok',
        'mem: mmap file ok',
        'mem: rss 128M ok',
        'mem: rss 1.9G warn!',
        'mem: slab alloc ok',
        'mem: slab free ok',
        'mem: slab cache full!',
        'mem: numa node0 ok',
        'mem: numa node1 ok',
        'mem: numa migrate ok',
        'mem: oom adj=-17',
        'mem: oom score=0',
        'mem: oom kill pid=9144!',
        'mem: reclaim 32M ok',
        'mem: dirty pages 512',
        'mem: dirty flush ok',
        'mem: hugepage alloc ok',
        'mem: hugepage fail!',
        'mem: thp split ok',
        'mem: swap out 8p',
        'mem: swap in 4p',
        'mem: balloon inflate ok',
        'mem: balloon deflate ok',
        'mem: guard page hit!',
        'mem: overflow detect!',
        'mem: underflow detect!',
        'fs: open ok fd=7',
        'fs: open fail ENOENT',
        'fs: fsync fd=7 ok',
        'fs: fsync fail EIO',
        'fs: inode alloc ok',
        'fs: inode free ok',
        'fs: dentry cache ok',
        'fs: dentry evict ok',
        'fs: dirty pages=256',
        'fs: dirty flush ok',
        'fs: read 4096B ok',
        'fs: write 4096B ok',
        'fs: write fail EIO!',
        'fs: mkdir ok',
        'fs: unlink ok',
        'fs: rename ok',
        'fs: chmod ok',
        'fs: chown ok',
        'fs: statvfs ok',
        'fs: quota exceed!',
        'fs: journal commit ok',
        'fs: journal abort!',
        'fs: snapshot ok',
        'fs: xattr set ok',
        'fs: xattr get ok',
        'arc: lz4 ratio=3.4 ok',
        'arc: lz4 ratio=2.1 ok',
        'arc: zstd ratio=4.2 ok',
        'arc: zstd ratio=3.8 ok',
        'arc: delta_encode ok',
        'arc: delta_decode ok',
        'arc: delta base miss',
        'arc: integrity sha256 ok',
        'arc: integrity sha256 fail!',
        'arc: index update ok',
        'arc: index corrupt!',
        'arc: extract ok',
        'arc: extract fail!',
        'arc: compress blk=128K',
        'arc: compress blk=64K',
        'arc: stream ok',
        'arc: stream abort!',
        'arc: checksum ok',
        'arc: checksum fail!',
        'arc: purge old ok',
        'jit: compile ok depth=4',
        'jit: compile fail depth=8',
        'jit: trace hot 0x5582',
        'jit: trace cold 0x1f00',
        'jit: inline depth=2 ok',
        'jit: inline depth=5 ok',
        'jit: deopt guard ok',
        'jit: deopt guard miss!',
        'jit: patch ok',
        'jit: patch fail!',
        'jit: evict code ok',
        'jit: cache full evict',
        'jit: cache hit ok',
        'jit: cache miss ok',
        'jit: tiered L1 ok',
        'jit: tiered L2 ok',
        'jit: tiered L3 ok',
        'jit: osr entry ok',
        'jit: bailout ok',
        'jit: stub call ok',
        'ipc: shm_create key=0x7712',
        'ipc: shm_detach key=0x7712',
        'ipc: mq_open qid=5 ok',
        'ipc: mq_recv qid=5 ok',
        'ipc: mq_close qid=5 ok',
        'ipc: sem_init set=1 ok',
        'ipc: sem_post set=1 ok',
        'ipc: sem_wait set=1 ok',
        'ipc: pipe create ok',
        'ipc: pipe read ok',
        'ipc: pipe write ok',
        'ipc: pipe close ok',
        'ipc: sock connect ok',
        'ipc: sock send ok',
        'ipc: sock recv ok',
        'ipc: sock close ok',
        'ipc: fifo open ok',
        'ipc: fifo read ok',
        'ipc: signal send ok',
        'ipc: signal recv ok',
        'sched: preempt ok pid=8812',
        'sched: preempt ok pid=9144',
        'sched: cgroup limit hit',
        'sched: cgroup ok',
        'sched: load avg 1m=2.4',
        'sched: load avg 5m=3.1',
        'sched: load avg 15m=2.8',
        'sched: migrate pid=3301',
        'sched: deadline ok',
        'sched: realtime ok',
        'sched: idle balance ok',
        'sched: throttle group ok',
        'sched: throttle group!',
        'sched: latency 4ms ok',
        'sched: latency 22ms warn',
        'sched: wakeup ok',
        'sched: sleep ok',
        'sched: yield ok',
        'sched: priority boost',
        'sched: priority restore',
        'pci: dma map ok',
        'pci: dma unmap ok',
        'pci: dma err!',
        'pci: dev 0:1c.0 ok',
        'pci: dev 0:1c.0 err!',
        'pci: msi setup ok',
        'pci: msi fire ok',
        'usb: dev 3-2.1 ok',
        'usb: dev 3-2.1 detach',
        'usb: xfer 1024B ok',
        'usb: xfer timeout!',
        'usb: reset ok',
        'usb: reset fail!',
        'hw: sensor temp=44C ok',
        'hw: sensor temp=78C warn',
        'hw: watchdog arm ok',
        'hw: watchdog bark!',
        'hw: gpio 12 set ok',
        'hw: gpio 14 clear ok',
        'hw: pwm duty=0.5 ok',
        'hw: spi xfer ok',
        'hw: i2c addr=0x48 ok',
        'hw: i2c nack!',
        'hw: uart rx ok',
        'hw: uart overflow!',
        'bci: ring5 impedance ok',
        'bci: ring6 impedance ok',
        'bci: ring7 impedance ok',
        'bci: drift correct ok',
        'bci: drift exceed warn',
        'bci: latency 3ms ok',
        'bci: latency 18ms warn',
        'bci: sync epoch=4413',
        'bci: sync epoch=4414',
        'bci: sync epoch=4415',
        'bci: sync epoch=4416',
        'bci: sync epoch=4417',
        'bci: sync epoch=4418',
        'bci: sync epoch=4419',
        'bci: sync epoch=4420',
        'bci: operator auth ok',
        'bci: operator deny!',
        'bci: foreign sig detect!',
        'bci: foreign sig block ok',
        'bci: waveform ok',
        'bci: waveform corrupt!',
        'bci: artifact removed ok',
        'bci: channel map ok',
        'bci: channel map err!',
        'bci: threshold adapt ok',
        'bci: threshold static ok',
        'bci: model update ok',
        'bci: model rollback ok',
        'bci: session start ok',
        'bci: session end ok',
        'bci: session log write',
        'net.tx corp-mirror:8443 800B',
        'net.rx corp-mirror:8443 600B',
        'net.tx 203.0.113.9:443 1400B',
        'net.rx 203.0.113.9:443 200B',
        'net.tx 198.51.100.4:9000 512B',
        'net.rx 198.51.100.4:9000 256B',
        'net.mtu 1500 ok',
        'net.mtu 9000 jumbo ok',
        'net.frag id=0x3a ok',
        'net.icmp echo ok',
        'net.icmp unreachable!',
        'net.arp resolve ok',
        'net.arp poison detect!',
        'net.ndp resolve ok',
        'net.ipv6 flow ok',
        'net.bgp update recv',
        'net.bgp withdraw recv',
        'net.ospf hello ok',
        'net.quic stream ok',
        'net.quic stream close',
        'net.http2 push ok',
        'net.http2 rst recv',
        'net.grpc call ok',
        'net.grpc timeout!',
        'net.websocket upgrade ok',
        'net.websocket ping ok',
        'net.websocket close ok',
        'sec: hsm connect ok',
        'sec: hsm derive ok',
        'sec: hsm sign ok',
        'sec: hsm fail!',
        'sec: acl check ok',
        'sec: acl deny!',
        'sec: acl update ok',
        'sec: mfa verify ok',
        'sec: mfa fail!',
        'sec: session create ok',
        'sec: session expire ok',
        'sec: session revoke ok',
        'sec: pin verify ok',
        'sec: pin fail!',
        'sec: biometric match ok',
        'sec: biometric fail!',
        'sec: threat score=0.12',
        'sec: threat score=0.87!',
        'sec: anomaly detect!',
        'sec: anomaly clear ok',
        'db: graph traverse ok',
        'db: graph traverse n=44',
        'db: graph path found',
        'db: graph path none',
        'db: graph cycle detect',
        'db: graph cycle break',
        'db: node_update ok',
        'db: node_merge ok',
        'db: edge_update ok',
        'db: edge_merge ok',
        'db: property set ok',
        'db: property del ok',
        'db: label add ok',
        'db: label del ok',
        'db: full_scan n=9871',
        'db: partial_scan n=412',
        'db: index_scan n=88',
        'db: merge sort ok',
        'db: hash join ok',
        'db: nested loop ok',
        'db: stats update ok',
        'db: stats stale warn',
        'corp: sync interval 30s',
        'corp: sync interval 5s',
        'corp: heartbeat ok',
        'corp: heartbeat miss!',
        'corp: config push ok',
        'corp: config pull ok',
        'corp: config reject!',
        'corp: identity check ok',
        'corp: identity deny!',
        'corp: credential ok',
        'corp: credential expire!',
        'corp: enforce cmd recv',
        'corp: enforce lockout ok',
        'corp: enforce lift ok',
        'glmz: time sync ok',
        'glmz: time drift +12ms',
        'glmz: time drift -8ms',
        'glmz: ntp ok',
        'glmz: ntp fail!',
        'glmz: freq 50.00Hz ok',
        'glmz: power stable ok',
        'glmz: power unstable!',
        'glmz: sensor array ok',
        'glmz: sensor fault d7',
        'glmz: sensor repair ok',
        'glmz: corridor open ok',
        'glmz: corridor seal!',
        'glmz: fab license ok',
        'glmz: fab license revoke!',
        'glmz: crest key ok',
        'glmz: crest key revoke!',
        'glmz: operator track ok',
        'glmz: operator evade!',
        'glmz: triangulate ok',
        'bci: impedance check ok',
        'bci: impedance fail ring2',
        'bci: signal quality 0.94',
        'bci: signal quality 0.41!',
        'bci: snr=28dB ok',
        'bci: snr=11dB warn',
        'bci: snr=6dB fail!',
        'bci: band_power ok',
        'bci: band_power low warn',
        'bci: event_rate 48Hz ok',
        'bci: event_rate 120Hz warn',
        'bci: event_rate overflow!',
        'bci: grip tight detect',
        'bci: grip loose detect',
        'bci: grip neutral ok',
        'bci: release detect ok',
        'bci: twitch filter ok',
        'bci: tremor filter ok',
        'bci: artifact epoch skip',
        'bci: artifact flag ok',
        'rbs: chamber check ok',
        'rbs: chamber empty!',
        'rbs: jam detect!',
        'rbs: jam clear ok',
        'rbs: misfire detect!',
        'rbs: misfire recover ok',
        'rbs: primer ok',
        'rbs: safety on ok',
        'rbs: safety off ok',
        'rbs: discharge cycle ok',
        'rbs: discharge blocked!',
        'rbs: feed interrupt!',
        'rbs: feed resume ok',
        'rbs: bolt cycle ok',
        'rbs: bolt jam!',
        'rbs: extractor ok',
        'rbs: ejector ok',
        'rbs: trigger check ok',
        'rbs: trigger fail!',
        'rbs: selector alpha ok',
        'net.tx sentinel:8443 640B',
        'net.rx sentinel:8443 320B',
        'net.tx beacon:9000 128B',
        'net.rx beacon:9000 64B',
        'net.tx ledger:5555 1024B',
        'net.rx ledger:5555 512B',
        'net.tx vault:8080 256B',
        'net.rx vault:8080 128B',
        'net.tx cipher:4433 768B',
        'net.rx cipher:4433 384B',
        'net.latency 2ms ok',
        'net.latency 44ms warn',
        'net.latency 180ms fail',
        'net.jitter 0.4ms ok',
        'net.jitter 8ms warn',
        'net.loss 0.001 ok',
        'net.loss 0.05 warn',
        'net.loss 0.20 fail!',
        'net.bandwidth 100Mbps ok',
        'net.bandwidth 1Gbps ok',
        'net.bandwidth 10Mbps low',
        'kern: module load ok',
        'kern: module unload ok',
        'kern: module deny!',
        'kern: syscall allow ok',
        'kern: syscall deny!',
        'kern: execve ok',
        'kern: clone ok',
        'kern: chdir ok',
        'kern: chroot ok',
        'kern: pivot_root ok',
        'kern: unshare ok',
        'kern: setns ok',
        'kern: iopl set ok',
        'kern: iopl deny!',
        'kern: mprotect ok',
        'kern: mprotect deny!',
        'kern: brk ok',
        'kern: mremap ok',
        'kern: remap_file_pages ok',
        'kern: userfaultfd ok',
        'sys: cron job ok',
        'sys: cron job fail!',
        'sys: timer create ok',
        'sys: timer expire ok',
        'sys: timer cancel ok',
        'sys: signal handler ok',
        'sys: signal mask ok',
        'sys: signal unblock ok',
        'sys: poll fd=7 ok',
        'sys: epoll event ok',
        'sys: epoll timeout ok',
        'sys: select ok',
        'sys: eventfd ok',
        'sys: timerfd ok',
        'sys: signalfd ok',
        'sys: pidfd ok',
        'sys: memfd create ok',
        'sys: memfd seal ok',
        'sys: userfaultfd reg ok',
        'sys: perf event ok',
        'mem: stack grow ok',
        'mem: stack overflow!',
        'mem: heap grow ok',
        'mem: heap shrink ok',
        'mem: mlock ok',
        'mem: munlock ok',
        'mem: madvise ok',
        'mem: mincore ok',
        'mem: msync ok',
        'mem: mprotect ok',
        'mem: mprotect fail!',
        'mem: pkey ok',
        'mem: pkey access deny!',
        'mem: asan report!',
        'mem: ubsan report!',
        'mem: valgrind clean',
        'mem: valgrind error!',
        'mem: kasan report!',
        'mem: kasan clear ok',
        'mem: leak detect!',
        'crypt: kdf ok',
        'crypt: kdf fail!',
        'crypt: salt gen ok',
        'crypt: iv gen ok',
        'crypt: nonce gen ok',
        'crypt: tag verify ok',
        'crypt: tag mismatch!',
        'crypt: poly1305 ok',
        'crypt: poly1305 fail!',
        'crypt: siphash ok',
        'crypt: siphash fail!',
        'crypt: scrypt ok 16M',
        'crypt: argon2id ok',
        'crypt: argon2id fail!',
        'crypt: bcrypt ok',
        'crypt: bcrypt fail!',
        'crypt: seal ok',
        'crypt: unseal ok',
        'crypt: unseal fail!',
        'crypt: padding ok',
        'arc: huffman ok',
        'arc: huffman fail!',
        'arc: snappy ratio=2.8',
        'arc: brotli ratio=5.1',
        'arc: zlib ratio=3.3',
        'arc: xz ratio=6.2',
        'arc: lzma ratio=5.8',
        'arc: patch apply ok',
        'arc: patch apply fail!',
        'arc: manifest verify ok',
        'arc: manifest fail!',
        'arc: rollback ok',
        'arc: rollback fail!',
        'arc: catalog update ok',
        'arc: catalog search ok',
        'arc: expiry check ok',
        'arc: expiry purge ok',
        'arc: dedup detect ok',
        'arc: dedup save 12M',
        'arc: cold storage ok',
        'jit: stub icall ok',
        'jit: stub vcall ok',
        'jit: stub dcall ok',
        'jit: regalloc ok',
        'jit: regalloc spill ok',
        'jit: constant fold ok',
        'jit: dead code elim ok',
        'jit: loop unroll ok',
        'jit: loop vectorize ok',
        'jit: loop peel ok',
        'jit: branch predict ok',
        'jit: branch mispred',
        'jit: function inline ok',
        'jit: function split ok',
        'jit: tail call ok',
        'jit: tail call elim ok',
        'jit: profile load ok',
        'jit: profile save ok',
        'jit: profile hot',
        'jit: profile cold',
        'ipc: semaphore ok',
        'ipc: semaphore timeout!',
        'ipc: mutex acquire ok',
        'ipc: mutex release ok',
        'ipc: mutex contended',
        'ipc: rwlock read ok',
        'ipc: rwlock write ok',
        'ipc: rwlock contended',
        'ipc: condvar wait ok',
        'ipc: condvar signal ok',
        'ipc: condvar broadcast ok',
        'ipc: barrier reach ok',
        'ipc: barrier wait ok',
        'ipc: futex wait ok',
        'ipc: futex wake ok',
        'ipc: futex contended',
        'ipc: spinlock ok',
        'ipc: spinlock contended',
        'ipc: atomic cmpxchg ok',
        'ipc: atomic fetch_add ok',
        'sched: nice -5 ok',
        'sched: nice +5 ok',
        'sched: ionice set ok',
        'sched: ioprio set ok',
        'sched: affinity cpu0 ok',
        'sched: affinity cpu3 ok',
        'sched: isolation ok',
        'sched: nohz ok',
        'sched: rcu barrier ok',
        'sched: srcu ok',
        'sched: rcu_read_lock ok',
        'sched: context switch ok',
        'sched: voluntary yield ok',
        'sched: involuntary preempt',
        'sched: workqueue ok',
        'sched: workqueue flush ok',
        'sched: kthread ok',
        'sched: kthread stop ok',
        'sched: softirq balance ok',
        'sched: napi poll ok',
        'glmz: grid sector 7A ok',
        'glmz: grid sector 4C ok',
        'glmz: grid sector 12B ok',
        'glmz: grid sector 9D ok',
        'glmz: checkpoint pass ok',
        'glmz: checkpoint fail!',
        'glmz: cam feed d7 ok',
        'glmz: cam feed d4 ok',
        'glmz: cam feed offline!',
        'glmz: audio monitor ok',
        'glmz: audio anomaly!',
        'glmz: bio scan pass ok',
        'glmz: bio scan fail!',
        'glmz: rf tag read ok',
        'glmz: rf tag unknown!',
        'glmz: permit valid ok',
        'glmz: permit expired!',
        'glmz: permit revoked!',
        'glmz: zone A access ok',
        'glmz: zone B access deny!',
        'corp: siem event ok',
        'corp: siem alert L2!',
        'corp: siem alert L4!',
        'corp: siem clear ok',
        'corp: dlp check ok',
        'corp: dlp block!',
        'corp: edr scan ok',
        'corp: edr detect!',
        'corp: edr quarantine ok',
        'corp: patch apply ok',
        'corp: patch fail!',
        'corp: vuln scan ok',
        'corp: vuln detect!',
        'corp: vuln remediate ok',
        'corp: incident open',
        'corp: incident close ok',
        'corp: forensic collect ok',
        'corp: forensic export ok',
        'corp: chain custody ok',
        'corp: chain custody fail!',
        // 40 new
        'dist12: relay → dist7 handoff ok',
        'bci: ring4 impedance 18kΩ within spec',
        'rbs: detent-cam 12ms lock ok',
        'hkb: fluid reservoir 88% full',
        'sec: vault key 0xBE04 re-sealed ok',
        'net.rx 10.44.12.3:8181 payload 88B ok',
        'kern: cpu0 load 4.4 temp 71C ok',
        'db: vacuum freed 1.2MB in 8s ok',
        'audit: uid=3301 zone-B entry logged',
        'jit: evict fn@0x44a2 cold 44 calls',
        'arc: delta-pack ratio=3.1 ok',
        'vm: remap 0x7ffe4100 prot=r-- ok',
        'ipc: proc/3301→proc/8812 msg ok',
        'lib: dlclose libneural.so.3 ok',
        'sig: SIGTERM pid:3301 → handled',
        'tls: session 0x44f2 renegotiated ok',
        'bci: batt 73% est 4.4h remain',
        'rbs: port-alpha confirmed 2.4ms',
        'sec: token 0xBE04 refreshed ok',
        'kern: softirq NET_RX 12Kpps ok',
        'dist9: node heartbeat 3.2ms ok',
        'bci: epoch=9 acc=98.1% commit',
        'rbs: yellow-led transit active',
        'hkb: piston rebound 8ms ok',
        'sec: enclave attest 0x9f3a ok',
        'net.syn 10.12.0.44:4412 ESTABLISHED',
        'db: edge-idx rebuild 4192 nodes ok',
        'glmz: gate-7 transit 0x4492 ok',
        'corp: incident-4412 closed ok',
        'bci: grip-cls conf=0.94 ok',
        'rbs: disc alpha→beta 38ms ok',
        'hkb: impulse spread 8ms→114ms ok',
        'kern: ksoftirqd cpu1 SCHED_OTHER',
        'net.rx 172.16.0.3:9090 HTTP 200',
        'sec: audit-trail 0x3301 flushed',
        'bci: calibrate ring3 Δ=+0.011',
        'ipc.gate: 0x22 [relay fwd enc]',
        'db: txn 0xDC44 commit 3.1ms ok',
        'arc: snapshot 0x44a2 verified',
        'jit: patch fn@0x5582 hot 812 calls',
        'bci: pair-key 0xA7F2 accepted',
        'rbs: spring-load 2.1N nominal',
        'hkb: gas-vent cycle ok 14ms',
        'net: beacon 0xBE04 heartbeat ok',
        'dist4: fab node up lat=9ms',
        'sec: kdf iter=200k derive ok',
        'bci: sweep-cls confirmed 0xAB ok',
        'kern: rcu grace period 2.4ms',
        'db: bloom-filter hit rate 91%',
        'corp: key-rotation vault ok',
        // 100 more
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
        'FATAL — bci ring4 contact lost',
        'FATAL — grip classifier crash',
        'FATAL — neural_rx overflow',
        'FATAL — bim_proc abort',
        'FATAL — cortex_sync timeout',
        'FATAL — rbs disc stuck',
        'FATAL — hkb piston jam',
        'FATAL — slug_feed empty',
        'FATAL — buck_feed jam',
        'FATAL — weapon thermal',
        'FATAL — net daemon abort',
        'FATAL — db corruption',
        'FATAL — sec vault fail',
        'FATAL — crypt engine abort',
        'FATAL — arc integrity fail',
        'FATAL — jit compile abort',
        'FATAL — ipc deadlock',
        'FATAL — sched starvation',
        'FATAL — fs journal abort',
        'FATAL — mem oom kill',
        'CRITICAL — bci key mismatch',
        'CRITICAL — rbs port fail',
        'CRITICAL — hkb buffer fail',
        'CRITICAL — neural flood',
        'CRITICAL — grip signal lost',
        'CRITICAL — bim classify fail',
        'CRITICAL — cortex desync',
        'CRITICAL — calibrate corrupt',
        'CRITICAL — epoch overflow',
        'CRITICAL — sweep lock fail',
        'CRITICAL — precision lock fail',
        'CRITICAL — weapon lockout',
        'CRITICAL — net bridge down',
        'CRITICAL — db txn abort',
        'CRITICAL — sec policy fail',
        'CRITICAL — crypt key corrupt',
        'CRITICAL — mem heap corrupt',
        'CRITICAL — fs dirty flush fail',
        'CRITICAL — sched deadline miss',
        'CRITICAL — ipc mutex corrupt',
        'PANIC — kernel null deref',
        'PANIC — kernel stack overflow',
        'PANIC — kernel double free',
        'PANIC — kernel use-after-free',
        'PANIC — kernel wild pointer',
        'PANIC — kernel irq storm',
        'PANIC — kernel rcu stall',
        'PANIC — kernel watchdog bark',
        'PANIC — kernel vm fault',
        'PANIC — kernel slab corrupt',
        'PANIC — kernel oops #5',
        'PANIC — kernel oops #11',
        'PANIC — kernel oops #14',
        'PANIC — kernel oops #7',
        'PANIC — kernel oops #9',
        'PANIC — kernel cpu lockup',
        'PANIC — kernel nmi',
        'PANIC — kernel mce',
        'PANIC — kernel ecc fail',
        'PANIC — kernel panic boot',
        'ABORT — signal 11 SIGSEGV',
        'ABORT — signal 6 SIGABRT',
        'ABORT — signal 4 SIGILL',
        'ABORT — signal 8 SIGFPE',
        'ABORT — signal 7 SIGBUS',
        'ABORT — signal 9 SIGKILL',
        'ABORT — signal 15 SIGTERM',
        'ABORT — bci_daemon crash',
        'ABORT — rbs_ctrl crash',
        'ABORT — hkb_mon crash',
        'ABORT — sec_daemon crash',
        'ABORT — net_daemon crash',
        'ABORT — db_daemon crash',
        'ABORT — jit_daemon crash',
        'ABORT — arc_daemon crash',
        'ABORT — fs assertion fail',
        'ABORT — mem assertion fail',
        'ABORT — crypt assertion fail',
        'ABORT — net assertion fail',
        'ABORT — corp daemon crash',
        'ERROR — bci ring impedance',
        'ERROR — bci snr low',
        'ERROR — bci waveform corrupt',
        'ERROR — bci model invalid',
        'ERROR — bci session fail',
        'ERROR — rbs selector fail',
        'ERROR — rbs cam read fail',
        'ERROR — rbs thermal limit',
        'ERROR — hkb pressure low',
        'ERROR — hkb spring fail',
        'ERROR — net tls handshake',
        'ERROR — net dns resolve',
        'ERROR — net conn refused',
        'ERROR — net timeout',
        'ERROR — net rst inject',
        'ERROR — db index corrupt',
        'ERROR — db repl lag',
        'ERROR — db lock timeout',
        'ERROR — db wal overflow',
        'ERROR — db snapshot fail',
        'KERNEL — page fault #14',
        'KERNEL — page fault #11',
        'KERNEL — page fault #7',
        'KERNEL — page not present',
        'KERNEL — write protect fault',
        'KERNEL — execute disable fault',
        'KERNEL — reserved bit fault',
        'KERNEL — SMEP violation',
        'KERNEL — SMAP violation',
        'KERNEL — NX violation',
        'KERNEL — double fault',
        'KERNEL — triple fault',
        'KERNEL — GPF #13',
        'KERNEL — invalid TSS',
        'KERNEL — stack fault',
        'KERNEL — alignment check',
        'KERNEL — machine check',
        'KERNEL — divide by zero',
        'KERNEL — invalid opcode',
        'KERNEL — device not avail',
        'CREST — rbs disc shattered',
        'CREST — rbs port mismatch',
        'CREST — bim chip fail',
        'CREST — bim key invalid',
        'CREST — bim model corrupt',
        'CREST — hkb hydraulic fail',
        'CREST — hkb spring break',
        'CREST — slug feed jam',
        'CREST — chamber obstruction',
        'CREST — thermal runaway',
        'CREST — led matrix fail',
        'CREST — pin actuator fail',
        'CREST — selector corrupt',
        'CREST — calibrate fail',
        'CREST — serial verify fail',
        'CREST — firmware corrupt',
        'CREST — firmware rollback',
        'CREST — cert expired',
        'CREST — cert revoked',
        'CREST — telemetry abort',
        'GLMZ — gate blacklist',
        'GLMZ — gate offline',
        'GLMZ — gate override',
        'GLMZ — gate seizure',
        'GLMZ — gate power fail',
        'GLMZ — district lockdown',
        'GLMZ — district quarantine',
        'GLMZ — dark node exposed',
        'GLMZ — relay down',
        'GLMZ — relay intercept',
        'GLMZ — comms blocked',
        'GLMZ — comms intercept',
        'GLMZ — beacon override',
        'GLMZ — permit revoked',
        'GLMZ — freelancer blacklist',
        'GLMZ — operator tracked',
        'GLMZ — operator triangulate',
        'GLMZ — fab license revoked',
        'GLMZ — corridor seal',
        'GLMZ — power grid fail',
        'CORP — policy violation',
        'CORP — policy override',
        'CORP — policy lockout',
        'CORP — audit log tamper',
        'CORP — audit gap detected',
        'CORP — mirror sync fail',
        'CORP — mirror conflict',
        'CORP — enclave breach',
        'CORP — enclave attest fail',
        'CORP — key rotation fail',
        'CORP — credential revoked',
        'CORP — enforcement alert',
        'CORP — enforcement lockout',
        'CORP — data exfil detect',
        'CORP — data exfil blocked',
        'CORP — lateral move detect',
        'CORP — config tamper',
        'CORP — license revoked',
        'CORP — territorial dispute',
        'CORP — incident escalated',
        'BCI — motor signal flood',
        'BCI — motor signal lost',
        'BCI — motor signal corrupt',
        'BCI — neural key fail',
        'BCI — neural key mismatch',
        'BCI — neural key corrupt',
        'BCI — grip classify fail',
        'BCI — grip signal lost',
        'BCI — grip pattern foreign',
        'BCI — calibration corrupt',
        'BCI — calibration timeout',
        'BCI — calibration abort',
        'BCI — foreign key injection',
        'BCI — denial-of-sensation',
        'BCI — interface lockout',
        'BCI — operator id mismatch',
        'BCI — operator id revoked',
        'BCI — ring contact fail',
        'BCI — epoch desync',
        'BCI — buffer overflow',
        'SEC — intrusion confirmed',
        'SEC — privilege escalation',
        'SEC — lateral move detected',
        'SEC — exfil in progress',
        'SEC — rootkit active',
        'SEC — rootkit variant new',
        'SEC — keylogger detect',
        'SEC — process inject detect',
        'SEC — dll inject detect',
        'SEC — mem scrape detect',
        'SEC — credential dump',
        'SEC — pass-the-hash',
        'SEC — token forge',
        'SEC — kerberoast detect',
        'SEC — golden ticket detect',
        'SEC — silver ticket detect',
        'SEC — dcSync detect',
        'SEC — supply chain alert',
        'SEC — zero day detect',
        'SEC — exploit attempt',
        'NET — glmz relay down',
        'NET — glmz relay intercept',
        'NET — district bridge fail',
        'NET — enforcement block',
        'NET — dns poison detect',
        'NET — arp spoof detect',
        'NET — bgp hijack detect',
        'NET — traffic analysis',
        'NET — bandwidth cap hit',
        'NET — tunnel collapse',
        'NET — tls downgrade detect',
        'NET — cert spoof detect',
        'NET — sni intercept',
        'NET — deep packet inspect',
        'NET — rate limit exceed',
        'NET — ddos detect',
        'NET — syn flood detect',
        'NET — reflection attack',
        'NET — amplification detect',
        'NET — covert channel',
        'DB — index corrupt',
        'DB — wal overflow',
        'DB — txn deadlock',
        'DB — replication lag',
        'DB — snapshot corrupt',
        'DB — page checksum fail',
        'DB — vacuum abort',
        'DB — integrity check fail',
        'DB — foreign key violate',
        'DB — constraint violate',
        'DB — unique violate',
        'DB — null violate',
        'DB — type mismatch',
        'DB — overflow detect',
        'DB — underflow detect',
        'DB — injection attempt',
        'DB — query timeout',
        'DB — connection pool exhaust',
        'DB — disk full',
        'DB — log full',
        'MEM — heap corruption',
        'MEM — stack smash',
        'MEM — buffer overflow',
        'MEM — buffer underflow',
        'MEM — use-after-free',
        'MEM — double free',
        'MEM — wild pointer',
        'MEM — null deref',
        'MEM — uninitialized read',
        'MEM — format string',
        'MEM — integer overflow',
        'MEM — integer underflow',
        'MEM — type confusion',
        'MEM — race condition',
        'MEM — oom kill',
        'MEM — slab corrupt',
        'MEM — page fault',
        'MEM — guard page hit',
        'MEM — pkey violation',
        'MEM — kasan report',
        'SYS — daemon crash',
        'SYS — service restart',
        'SYS — service fail',
        'SYS — watchdog expire',
        'SYS — reboot forced',
        'SYS — shutdown abnormal',
        'SYS — fork fail',
        'SYS — exec fail',
        'SYS — socket fail',
        'SYS — pipe fail',
        'SYS — timer fail',
        'SYS — cron fail',
        'SYS — cgroup limit',
        'SYS — fd leak detect',
        'SYS — handle leak detect',
        'SYS — thread leak detect',
        'SYS — zombie accumulate',
        'SYS — orphan process',
        'SYS — init fail',
        'SYS — mount fail',
        'SIGNAL — SIGSEGV pid=8812',
        'SIGNAL — SIGABRT pid=9144',
        'SIGNAL — SIGILL pid=3301',
        'SIGNAL — SIGFPE pid=7712',
        'SIGNAL — SIGBUS pid=4492',
        'SIGNAL — SIGKILL pid=8812',
        'SIGNAL — SIGTERM pid=9144',
        'SIGNAL — SIGUSR1 abort',
        'SIGNAL — SIGUSR2 abort',
        'SIGNAL — SIGPIPE pid=3301',
        'SIGNAL — SIGHUP abort',
        'SIGNAL — SIGALRM expire',
        'SIGNAL — SIGCHLD abort',
        'SIGNAL — SIGXCPU exceeded',
        'SIGNAL — SIGXFSZ exceeded',
        'SIGNAL — SIGVTALRM expire',
        'SIGNAL — SIGPROF expire',
        'SIGNAL — SIGTRAP debug',
        'SIGNAL — SIGWINCH ignore',
        'SIGNAL — SIGSYS sandboxed',
        'RACE — data race detect',
        'RACE — lock order invert',
        'RACE — toctou detect',
        'RACE — aba problem detect',
        'RACE — write-write race',
        'RACE — read-write race',
        'RACE — iterator race',
        'RACE — destructor race',
        'RACE — init race detect',
        'RACE — singleton race',
        'RACE — cache race detect',
        'RACE — counter race detect',
        'RACE — flag race detect',
        'RACE — pointer race detect',
        'RACE — refcount race',
        'RACE — tsan report',
        'RACE — helgrind detect',
        'RACE — drd detect',
        'RACE — memcheck detect',
        'RACE — thread sanitizer',
        'LOCK — deadlock detected',
        'LOCK — livelock detected',
        'LOCK — mutex timeout',
        'LOCK — rwlock timeout',
        'LOCK — semaphore timeout',
        'LOCK — condvar timeout',
        'LOCK — barrier timeout',
        'LOCK — futex timeout',
        'LOCK — spinlock timeout',
        'LOCK — lock hierarchy violate',
        'LOCK — priority inversion',
        'LOCK — convoy detect',
        'LOCK — thundering herd',
        'LOCK — starvation detect',
        'LOCK — recursive deadlock',
        'LOCK — cross-process deadlock',
        'LOCK — distributed deadlock',
        'LOCK — order violation',
        'LOCK — cycle detect',
        'LOCK — orphan lock detect',
        'IO — disk write fail',
        'IO — disk read fail',
        'IO — disk timeout',
        'IO — disk full',
        'IO — disk bad sector',
        'IO — disk ecc fail',
        'IO — disk smart warn',
        'IO — disk smart fail',
        'IO — raid degraded',
        'IO — raid fail',
        'IO — network timeout',
        'IO — network reset',
        'IO — network unreachable',
        'IO — usb transfer fail',
        'IO — pci dma fail',
        'IO — i2c nack',
        'IO — spi timeout',
        'IO — uart overflow',
        'IO — gpio fault',
        'IO — pwm fault',
        'FATAL — bci epoch overflow',
        'FATAL — rbs emergency stop',
        'FATAL — hkb thermal limit',
        'FATAL — net corp mirror fail',
        'FATAL — sec vault locked',
        'FATAL — db disk full',
        'FATAL — mem slab exhaust',
        'FATAL — fs journal corrupt',
        'FATAL — jit code cache full',
        'FATAL — arc checksum fail',
        'CRITICAL — bci operator deny',
        'CRITICAL — rbs led fail',
        'CRITICAL — hkb spring fail',
        'CRITICAL — net beacon spoof',
        'CRITICAL — sec hsm fail',
        'CRITICAL — db graph corrupt',
        'CRITICAL — mem guard hit',
        'CRITICAL — fs inode corrupt',
        'CRITICAL — jit bail fail',
        'CRITICAL — arc delta fail',
        'PANIC — kernel watchdog nmi',
        'PANIC — kernel cpu deadlock',
        'PANIC — kernel oops cascading',
        'PANIC — kernel dma storm',
        'PANIC — kernel irq cascade',
        'PANIC — kernel io storm',
        'PANIC — kernel pcie error',
        'PANIC — kernel memory storm',
        'PANIC — kernel rcu timeout',
        'PANIC — kernel hung task',
        'ABORT — bci daemon assert',
        'ABORT — rbs controller abort',
        'ABORT — hkb monitor abort',
        'ABORT — net proxy abort',
        'ABORT — sec scanner abort',
        'ABORT — db journal abort',
        'ABORT — mem allocator abort',
        'ABORT — fs flusher abort',
        'ABORT — jit executor abort',
        'ABORT — arc verifier abort',
        'ERROR — bci contact ring3',
        'ERROR — rbs thermal trip',
        'ERROR — hkb fluid empty',
        'ERROR — net tunnel collapse',
        'ERROR — sec cert expired',
        'ERROR — db replica fail',
        'ERROR — mem pkey fault',
        'ERROR — fs sync fail',
        'ERROR — jit trace fail',
        'ERROR — arc index missing',
        'KERNEL — tlb flush fail',
        'KERNEL — cr3 invalid',
        'KERNEL — cr0 corrupt',
        'KERNEL — gdtr corrupt',
        'KERNEL — idtr corrupt',
        'KERNEL — idt gate invalid',
        'KERNEL — gdt segment fault',
        'KERNEL — ldt fault',
        'KERNEL — tss descriptor fail',
        'KERNEL — pt walk fail',
        'CREST — weapon id spoof',
        'CREST — weapon ban active',
        'CREST — telemetry block',
        'CREST — calibrate tamper',
        'CREST — round counter spoof',
        'CREST — discharge blocked',
        'CREST — sensor array fail',
        'CREST — actuator fault',
        'CREST — motor controller fail',
        'CREST — power rail fault',
        'GLMZ — sensor array fail',
        'GLMZ — corridor offline',
        'GLMZ — checkpoint breach',
        'GLMZ — zone A quarantine',
        'GLMZ — zone B lockdown',
        'GLMZ — cam offline d4',
        'GLMZ — cam offline d7',
        'GLMZ — audio anomaly d12',
        'GLMZ — bio scan fail d9',
        'GLMZ — rf jamming detect',
        'CORP — siem alert critical',
        'CORP — dlp breach detect',
        'CORP — edr isolate host',
        'CORP — vuln exploit detect',
        'CORP — zero day active',
        'CORP — supply chain breach',
        'CORP — insider threat detect',
        'CORP — data destruction detect',
        'CORP — ransomware detect',
        'CORP — apt persist detect',
        'BCI — cortex signal null',
        'BCI — cortex signal overload',
        'BCI — intent state corrupt',
        'BCI — intent transition fail',
        'BCI — bim chip overheat',
        'BCI — bim power rail fail',
        'BCI — neural band corrupt',
        'BCI — theta spike flood',
        'BCI — beta burst overflow',
        'BCI — gamma artifact flood',
        'SEC — zero day confirmed',
        'SEC — apt active detect',
        'SEC — ransomware active',
        'SEC — data wipe detect',
        'SEC — firmware tamper',
        'SEC — bootkit detect',
        'SEC — hypervisor attack',
        'SEC — vm escape detect',
        'SEC — container escape',
        'SEC — kernel exploit',
        // 40 more
        'FATAL — neural bridge dropout',   'FATAL — bci handshake refused',   'FATAL — rbs spring shatter',
        'FATAL — hkb valve blowout',       'FATAL — glmz gate auth fail',     'FATAL — relay blackhole loop',
        'FATAL — sec key store corrupt',   'FATAL — db redo log corrupt',     'FATAL — vm page table fault',
        'FATAL — jit segment overflow',    'FATAL — ipc ring overflow',       'FATAL — tls session bomb',
        'CRITICAL — bci flood attack',     'CRITICAL — rbs cam desync',       'CRITICAL — hkb rack jam',
        'CRITICAL — glmz perimeter breach','CRITICAL — corp exfil volume',    'CRITICAL — sec replay storm',
        'CRITICAL — db wal storm',         'CRITICAL — net arp poison',       'CRITICAL — vm oom cascade',
        'CRITICAL — jit cache corrupt',    'CRITICAL — ipc deadlock global',  'CRITICAL — arc block missing',
        'ABORT — bci ring6 fault',         'ABORT — rbs torque limit',        'ABORT — hkb actuator stall',
        'ABORT — glmz feed desync',        'ABORT — corp broker timeout',     'ABORT — sec vault gone',
        'ERROR — bci epoch mismatch',      'ERROR — rbs position lost',       'ERROR — hkb fluid leak',
        'ERROR — glmz relay loop',         'ERROR — corp audit missing',      'ERROR — sec cert chain broken',
        'KERNEL — slab poison detect',     'KERNEL — hpet timer fault',       'KERNEL — cpuid mismatch',
        'CREST — auth module unresponsive','GLMZ — district feed blackout',   'BCI — classifier poison inject',
        'SEC — bci exploit detected',      'PANIC — kernel dma storm',        'PANIC — irq flood 64k/s',
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
        'BCI ring4 contact lost: impedance=INF\nADC channel 4 reading 0 — hardware fault\nCalibration aborted — reattach and retry',
        'BCI ring7 contact fail: impedance=28kΩ\nThreshold exceeded — signal unreliable\nRing replacement required before next session',
        'BCI snr=6dB below minimum threshold\nNoise floor elevated — EM interference?\nClassifier confidence 0.21 — unsafe',
        'BCI waveform corrupt: epoch 4412\nExpected 48Hz motor band — received garbage\nCalibration state invalidated — reset required',
        'BCI model mismatch: expected v3.4 got v2.1\nClassifier weights incompatible with hardware\nFalling back to null mode — weapon safe',
        'BCI operator id mismatch: got 0x7712\nExpected 0x3301 per corp credential store\nSession denied — escalating to sec daemon',
        'BCI neural key verify fail: PBKDF2 mismatch\nSalt at byte offset 16 corrupted\nKey unrecoverable — operator must re-enroll',
        'BCI grip pattern foreign: profile 0x9F not known\nBIM classifier confidence 0.08\nWeapon lockout enforced — threat protocol',
        'BCI intent transition fail: SWEEP→PRECISION\nDisc rotation timeout after 300ms\nHKB impulse correction skipped — unsafe',
        'BCI delta_band 13Hz absent: epoch 4418\nMotor cortex signal not detected\nOperator fatigue? Recalibrate immediately',
        'BCI theta spike: 120μV at ring2\nArtifact flood — epoch discarded\nFiltering engaged — 3 epochs consecutive bad',
        'BCI gamma artifact flood: 800Hz component\nLow-pass filter overwhelmed\nADC saturation suspected — hardware check',
        'BCI beta burst overflow: queue depth=512\nSlot drain falling behind 220ms\nBuffer eviction triggered — 48 events lost',
        'BCI calibration timeout: 120s elapsed\nOperator did not complete sweep/precision cycle\nSession aborted — locked until manual reset',
        'RBS disc rotation timeout: 300ms exceeded\nPort selector stuck at intermediate position\nEmergency stop engaged — both ports locked',
        'RBS port mismatch: bim_cmd=BETA got ALPHA\nSelector position sensor failure\nWeapon discharged in wrong mode — incident',
        'RBS thermal 78C: critical threshold\nDisc bearing lubricant breakdown suspected\nCooldown enforced — 300s lockout active',
        'RBS chamber obstruction detected\nFeed pressure sensor: 0 psi — blockage\nClear procedure required — do not force',
        'RBS led matrix fail: driver 0x3A timeout\nI2C NACK on address 0x48\nStatus indication lost — manual mode required',
        'HKB hydraulic pressure 0 psi\nFluid reservoir empty — sensor confirmed\nSpring-only mode: recoil compensation 40%',
        'HKB piston jam at position 2.8mm\nActuator current 4.2A — stall detected\nCycle aborted — manual extraction required',
        'HKB spring break: coil 3 fractured\nResonance frequency shift +12Hz\nImpulse correction degraded — unsafe',
        'HKB wear_score=0.94: service overdue\nBearing play 0.8mm above tolerance\nOperation suspended pending maintenance',
        'HKB thermal runaway: temp 91C\nFriction coefficient elevated 3x nominal\nEmergency stop — cooldown 600s required',
        'KASAN: use-after-free in bci_classify()\nRead of size 8 at addr ffff888201c4a000\nBuggy address freed at bci_session_end+0x44',
        'KASAN: heap-buffer-overflow in grip_cls()\nWrite of size 4 at addr ffff888103b2c080\nObject bounds exceeded by 4 bytes',
        'KASAN: stack-buffer-overflow in neural_rx()\nWrite of size 64 at addr ffffc9000481be80\nStack frame overflowed — backtrace corrupt',
        'KASAN: null-ptr-deref in bim_calibrate()\nRead of size 8 at addr 0000000000000008\nNull dereference — operator struct not init',
        'KASAN: slab-out-of-bounds in epoch_parse()\nRead of size 16 at addr ffff88810de40200\nOverread by 8 bytes — heap object exhausted',
        'AddressSanitizer: heap-use-after-free\nFree at: bci_session_close+0x22\nUse at: grip_classify+0x8f — UAF confirmed',
        'AddressSanitizer: global-buffer-overflow\nRead at: neural_feature_vec+0x3c\nOffset 2052 of size 2048 — overflow 4B',
        'AddressSanitizer: stack-use-after-return\nVariable at: epoch_handler frame+0x40\nFrame deallocated before use — corruption',
        'UndefinedBehaviorSanitizer: signed overflow\nbci_delta_calc at bci_math.c:88 col:12\nResult: -2147483648 + -1 — undefined',
        'ThreadSanitizer: data race on bim_state\nWrite at: rbs_rotate+0x2a (thread 2)\nRead at: bci_classify+0x5f (thread 1)',
        'TCP RST injected by enforcement node\nconn 10.44.7.3:8443 → 172.16.0.1:443\nActive interception confirmed — abort',
        'TLS handshake fail: cert verify error\nCN=glmz.relay.internal depth=1\nCERT_UNTRUSTED — relay cert rotated?',
        'TLS downgrade to 1.1 attempted\nPeer hello: max_version=TLS1.1\nDowngrade rejected — handshake aborted',
        'DNS resolve fail: glmz-relay.internal\nNXDOMAIN after 3 retries — node gone?\nFailing over to backup 10.44.0.254',
        'DNS poison detected: glmz.relay.internal\nExpected A 10.44.7.3 got 203.0.113.99\nCache flushed — upstream tampered',
        'ARP spoof detected: 10.44.0.1\nExpected MAC aa:bb:cc:dd:ee:ff\nGot 11:22:33:44:55:66 — MITM suspected',
        'BGP hijack detected: prefix 10.44.0.0/16\nOrigin AS 65001 unexpected — not glmz\nAll routes via AS 65001 withdrawn',
        'WireGuard tunnel collapse: peer 10.44.7.3\nHandshake timeout after 5 retries\nRekeying failed — traffic blackholed',
        'Net bridge dist4→dist7 down\nBoth endpoints unreachable\nFalling back to mesh route — 440ms penalty',
        'DDOS detected: 1.2Mpps SYN flood\nSource: spoofed 198.51.100.0/24\nSyn cookies activated — degraded mode',
        'CORP exfil: 4.8MB to 203.0.113.44 port 443\nDestination not in allowlist\nDLP blocked — incident ticket #4412',
        'CORP audit gap: 180 events missing\nTimestamp 2026-04-12T02:14 to T02:17\nLog integrity broken — forensic needed',
        'CORP key rotation fail: vault unreachable\nHSM connection timeout after 30s\nFalling back to cached key — expires in 4h',
        'CORP enforcement lockout: uid=0x3301\nPolicy version 4.2: district 7 denied\nAll credentials suspended — escalated',
        'CORP mirror conflict: node graph delta\n3 edge records diverged from primary\nManual reconciliation required',
        'SEC intrusion: pid=8812 uid=0\nUnexpected root shell from bci_daemon\nProcess isolated — forensic capture started',
        'SEC privilege escalation: uid=1001→0\nSUIDexploit in /usr/bin/pkexec suspected\nCVE-2021-4034 pattern — patch missing',
        'SEC lateral move: 10.44.7.3→10.44.4.11\nRDP brute force 2847 attempts in 60s\nBlocked at enforcer — source blacklisted',
        'SEC rootkit detected: /proc/kallsyms diff\n14 syscall table entries modified\nReboot to clean kernel required',
        'SEC exfil: DNS tunneling detected\nTXT queries 4.4kB avg — abnormal\nDNS resolver rate-limited — tunnel broken',
        'GLMZ gate 44 blacklisted: uid=0x3301\nActive enforcement warrant ID 0x0099\nAll transit points notified — lockdown',
        'GLMZ dark node 10.44.7.99 exposed\nNode appeared in topology scan\nOperator 0x7712 location triangulated d7',
        'GLMZ district 9 lockdown active\nEnforcement beacon 0xBE04 broadcasting\nbci_daemon forced offline — remote cmd',
        'GLMZ corridor B sealed: intrusion alarm\nMotion + thermal sensors triggered simultaneously\nPersonnel containment procedure active',
        'GLMZ relay intercept: traffic redirected\nconn 10.44.7.3:9000 rerouted to 10.44.99.1\nDeep packet inspection active — abort',
        'NullReferenceException in BimCalibrate()\nObject reference not set: calibrationCtx\nStack: BimCalibrate+0x2c BciSession+0x88',
        'NullReferenceException in RbsRotate()\nObject reference not set: discController\nStack: RbsRotate+0x14 WeaponCore+0x4f',
        'IndexOutOfRangeException: ring array\nIndex 8 is out of bounds [0..7]\nStack: RingReader+0x22 BciPoll+0x3a',
        'OverflowException: neural_event_count\nInt32 max exceeded: accumulator=2147483647\nCounter wrap — epoch data invalid',
        'InvalidOperationException: rbs_disc_lock\nDisc locked in transitioning state\nDeadlock between BIM and RBS threads',
        'ObjectDisposedException: bci_session\nSession used after Dispose() called\nUse-after-dispose — ref leak suspected',
        'StackOverflowException: neural_classify\nRecursion depth 10000 exceeded\nbci_daemon pid=8812 terminated — restart',
        'OutOfMemoryException: feature_buffer\nFailed to allocate 67108864 bytes\nHeap pressure — GC unable to collect',
        'CryptographicException: AES-GCM tag fail\nAuthentication tag mismatch — tampered?\nMessage discarded — connection reset',
        'IOException: wal_write fd=7\nerrno=28 ENOSPC — disk full\nDatabase suspended — all writes blocked',
        'Segfault in libneural.so.3+0x4f3a\nAddress 0x00000000000000a8 not mapped\nCore dumped to /tmp/core.8812',
        'Segfault in librbs.so.2+0x2c11\nAddress 0x00000000000000b8 not mapped\nCore dumped to /tmp/core.9144',
        'Segfault in libhkb.so.1+0x1a44\nNull pointer deref in HkbCycle()\nCore dumped to /tmp/core.3301',
        'Double free in tcache bin 3\nChunk 0x5582c4a0 freed twice\nHeap integrity check failed — abort',
        'Stack smashing in neural_rx_handler()\n__stack_chk_fail: canary corrupted\nAbort trap 6 — pid=8812',
        'Stack smashing in grip_cls_loop()\n__stack_chk_fail: canary corrupted\nAbort trap 6 — pid=9144',
        'Heap corruption: free list poisoned\nFast bin fd pointer 0x5582c4b0 invalid\nAbort — heap integrity check failed',
        'EXCEPTION_ACCESS_VIOLATION 0xc0000005\nWrite to 0xffffffffffffffff\nModule: bim_core.dll +0x3a88',
        'EXCEPTION_STACK_OVERFLOW 0xc00000fd\nThread stack exhausted\nModule: neural_rx.dll at 0x7ff8+0x1234',
        'EXCEPTION_ILLEGAL_INSTRUCTION 0xc000001d\nUnknown opcode at 0x7ff8bim+0x88\nPossible code injection detected',
        'Kernel oops #5: BUG at bci_mm.c:1402\nKernel BUG — write to read-only page\nIP: bci_mmap_fault+0x44 CR2: 0x5582c400',
        'Kernel oops #11: bad page state\nPFN 0x1a440 flags 0x00000000\nIP: free_pages_check+0x2c',
        'Kernel oops #14: page fault\nCR2: 0x00000000000000a8\nIP: bci_classify+0x3f PGD 0 P4D 0',
        'Kernel NMI: hardware error\nMCE bank 0: MCGSTATUS=7\nMCI_STATUS=0xbe00000000000800',
        'Kernel MCE: uncorrectable DRAM ECC\nBank 1: addr=0x1a4400000 misc=0x88\nMemory offline — degraded operation',
        'Kernel watchdog: CPU0 hard lockup\nRIP: _raw_spin_lock+0x28\nHeld lock: bci_spinlock — deadlock?',
        'Kernel RCU stall: CPU 2\nrcu_sched kthread starved for 21s\nAll RCU callbacks delayed — system degraded',
        'Kernel hung task: bci_flusher\nTask blocked for 122s on semaphore\nCall trace: bci_flush_wait+0x3c',
        'OOM killer: kill process 8812 (bci_daemon)\nom_score_adj=0 anon-rss=1932M\nOut of memory: force killed',
        'OOM killer: kill process 9144 (neural_rx)\noom_score_adj=500 anon-rss=440M\nSystem memory critically low',
        'Meltdown: KPTI disabled — kernel exposed\nSpectre v2: IBRS not active\nSystem vulnerable — patch not applied',
        'Spectre-v1 gadget in bci_classify()\nArray index bounds not enforced\nSpeculative load gadget at +0x3c',
        'Spectre-v4: store bypass possible\nbci_session_ctx speculative read\nSSBD not enabled — memory poisoning risk',
        'CORP ransomware: mass encryption active\n44 files encrypted in 3s — CryptoLocker variant\nIsolate and restore from backup',
        'CORP apt: persistent backdoor installed\nRegistry RunKey bci_svc added\nC2 beacon to 203.0.113.99:443 detected',
        'CORP zero-day: CVE-2026-XXXX active\nLibneural.so heap spray at +0x3a00\nPatch not available — compensating control',
        'CORP supply chain: package tamper\nlibneural.so.3 checksum mismatch\nExpected sha256 a1b2c3 got 99ff00 — abort',
        'CORP insider threat: bulk download\nOperator 0x3301 downloaded 2.4GB in 8m\nDLP alert — session suspended pending review',
        'DB integrity check failed: page 4412\nChecksum mismatch — expected 0x3a44 got 0xff00\nTable bci_events corruption confirmed',
        'DB wal overflow: segment 88 full\nWAL size exceeded 1GB limit\nCheckpoint forced — writes blocked 30s',
        'DB deadlock: txn 3301 and txn 7712\nCycle: txn3301 waits for txn7712\ntxn7712 waits for txn3301 — victim chosen',
        'DB replication lag: 8.4s behind primary\nNetwork throughput 2.1MB/s vs 12MB/s needed\nReplica paused — failover risk',
        'DB snapshot corrupt: epoch 88\nHeader magic 0xDEADBEEF unexpected\nFalling back to WAL replay from epoch 44',
        'DB injection attempt: table bci_events\nInput: 1 OR 1=1 -- detected\nQuery aborted — audit log written',
        'DB disk full: /var/db/bci 0B free\nWrites suspended — emergency cleanup\nPurge archives > 30d to free space',
        'DB connection pool exhausted: max=64\nAll connections busy — queue depth 128\nNew requests rejected — backpressure active',
        'FS journal abort: block device error\nerrno=EIO on /dev/sda1 at sector 441200\nFilesystem mounted read-only — repairs needed',
        'FS inode table corrupt: group 12\nInode 44120 magic number invalid\nfsck required before remount',
        'FS dirty pages overflow: 65536 pending\nFlusher thread behind 22s\nKernel may panic — emergency sync',
        'FS quota exceeded: uid=1001\n20GB hard limit reached — 0B free\nAll writes rejected until purge',
        'FS bad block: /dev/sdb1 sector 441200\nRead EIO — sector marked bad\nFilesystem degraded — backup immediately',
        'MEM guard page violation: addr 0x7fff4000\nWrite to non-writable guard page\nStack overflow suspected — abort',
        'MEM pkey violation: pkey=3 addr 0x5582c000\nAccess not permitted for current pkey\nSegfault — process terminated',
        'MEM asan report: heap-buffer-overflow\nWrite of 4 past end of 2048B buffer\nStack: bci_ring_write+0x3a grip_cls+0x88',
        'MEM ubsan: integer overflow at bci_math.c:44\nSigned overflow: 2147483647 + 1\nUndefined behavior — value unreliable',
        'MEM leak detected: 44 objects unreachable\nTotal leaked: 1.2MB in 120s\nLeaking from bci_session_alloc+0x22',
        'CRYPT AES-GCM tag verify fail\nExpected tag a1b2c3d4e5f67890\nGot tag 0000000000000000 — tampered packet',
        'CRYPT ECDH derive fail: invalid point\nPeer public key not on curve P-256\nPossible attack — connection aborted',
        'CRYPT HMAC mismatch: neural telemetry\nExpected HMAC a1b2c3 got d4e5f6\nMessage integrity violated — abort',
        'CRYPT RNG entropy low: 4 bits available\n/dev/random exhausted — blocking\nKey generation paused — entropy needed',
        'CRYPT nonce wrap imminent: counter=2^32-8\nChacha20 nonce exhaustion in 8 messages\nRekeying required immediately',
        'CRYPT PBKDF2 fail: salt corrupt at byte 32\nDerived key all-zero — unusable\nNeural key unrecoverable — factory reset',
        'CRYPT ed25519 verify fail: sig mismatch\nPublic key 0x3301 signature invalid\nPossible forgery — session rejected',
        'CRYPT RSA decrypt fail: padding error\nPKCS#1 v1.5 padding invalid\nOracle attack possible — switch to OAEP',
        'CRYPT GCM nonce reuse detected\nSame nonce used for 2 messages\nKey stream exposed — immediate rekey',
        'CRYPT argon2id fail: memory 65536KB\nOut of memory during KDF\nKey derivation aborted — retry with less mem',
        'ARC integrity fail: sha256 mismatch\nExpected a1b2c3d4 got 99ff0011\nArchive corrupted — restore from backup',
        'ARC delta decode fail: base missing\nBase hash 0x3a44 not in catalog\nFull snapshot required — incremental broken',
        'ARC index corrupt: entry 4412\nMagic byte 0xDEAD unexpected\nCatalog rebuild required — catalog.json lost',
        'ARC checksum fail: block 88 lz4\nDecompressed hash mismatch\nBlock corrupt — cannot decompress safely',
        'ARC rollback fail: target epoch 44\nSnapshot not found in cold storage\nData loss risk — manual recovery needed',
        'JIT compile fail: depth 8 exceeded\nInlining budget exceeded at call +0x88\nFalling back to interpreter — 10x slower',
        'JIT deopt guard miss: assumption violated\nType check failed at bim_classify+0x3c\nDeopt triggered — recompile queued',
        'JIT code cache full: 64MB exhausted\nEviction failed — no cold entries\nCompilation suspended — interpreter mode',
        'JIT illegal instruction generated\nOpcode 0x0F0B (UD2) at trace+0x44\nJIT bug — process terminated',
        'JIT trace abort: infinite loop detected\nBack edge count 100000 exceeded\nTrace compilation cancelled',
        'IPC deadlock: sem 0 and sem 1\nbci_thread holds sem0 waits sem1\nrbs_thread holds sem1 waits sem0',
        'IPC pipe broken: SIGPIPE pid=8812\nRead end closed before write\nWriter terminated — data lost',
        'IPC shared mem corrupt: key=0x3301\nMagic header invalid\nAttach rejected — reinitialize required',
        'IPC message queue overflow: qid=4\nQueue depth 1024 full — messages lost\nConsumer too slow — backpressure needed',
        'IPC socket connection reset: fd=7\nPeer closed unexpectedly\nIn-flight data lost — retry required',
        'SCHED deadlock: cpu0 and cpu1 locked\nBoth waiting on cross-cpu lock\nWatchdog triggered — reboot forced',
        'SCHED starvation: pid=3301 bci_daemon\n480s without CPU time\nCgroup throttle too aggressive — tune',
        'SCHED latency 220ms: bci_classify\nDeadline missed — grip decision delayed\nWeapon mode indeterminate — abort',
        'SCHED cgroup limit: neural_rx 100ms\nBurst consumed — throttled to 50ms\nEvent processing 3x slower',
        'SCHED cpu lockup: CPU3 hard lockup\nRIP: bci_spinlock+0x18\nNMI triggered — core offline',
        'PCI DMA error: dev 0:1b.0\nDMA read timeout — IOMMU fault\nDevice reset required — PCIe link degraded',
        'USB transfer timeout: dev 3-1.2 ep 0x81\nTransfer timeout after 5s\nDevice disconnected — reconnect required',
        'HW temperature 91C: cpu0 throttled\nFrequency reduced to 800MHz\nbci_daemon latency 10x nominal',
        'HW i2c NACK: addr 0x48 (rbs led)\nNo acknowledgement after 3 retries\nLED matrix offline — status indication lost',
        'HW uart overflow: rx fifo full\nIncoming data rate 2Mbps > 115200\nData lost — baud rate mismatch',
        'BCI ring4 contact lost: ADC=0 impedance=INF\nElectrode gel dried — hardware fault\nCalibration aborted — session terminated',
        'BCI neural_rx overflow: queue=4096 full\n220 events dropped in last 100ms\nClassifier state stale — weapon locked',
        'BCI epoch 4419 desync: timestamp gap +3.2s\nExpected delta 20ms got 3200ms\nSession continuity broken — reauth needed',
        'BCI foreign injection: pattern 0x9F12 recv\nUnknown operator profile in BIM\nSecurity lockout engaged — incident #4412',
        'BCI motor cortex signal null: ring 0-3 zero\nAll four rings reading baseline\nGlove sensor array offline — check conn',
        'GLMZ enforcement warrant: ID 0x3301\nActive warrant for unauthorized d4 entry\nAll transit nodes alerted — capture order',
        'GLMZ operator triangulated: 0x7712\nThree beacon bearings intersect d7-C4\nLocation fix 8m accuracy — team dispatched',
        'GLMZ comms intercept: relay 10.44.0.99\nTraffic redirected to inspection node\nAll plaintext data captured — abort',
        'GLMZ zone B lockdown: intrusion alarm\nThree simultaneous sensor triggers\nPersonnel immobilization protocol active',
        'GLMZ cam network offline: district 4\n14 of 16 cameras unreachable\nSabotage suspected — backup feeds only',
        'DB graph cycle detected: node 3301\nPath: 3301→7712→4492→3301\nCycle breaks topological ordering — abort',
        'DB foreign key violation: edge table\nReferenced node 0x3301 deleted\nCascade blocked — orphan edges remain',
        'DB type mismatch: column epoch INT64\nGot string "4412a" — parse error\nRow rejected — insert aborted',
        'DB lock timeout: txn 8812 waited 30s\nLock held by txn 9144 uncommitted\nTimeout abort — retry required',
        'DB vacuum abort: disk I/O error\nPage 4412 read EIO — sda1 fault\nVacuum incomplete — fragmentation grows',
        'NET beacon spoof: enf node 0xBE04\nRogue beacon transmitting on freq 915MHz\nFreelancers being lured — avoid district 7',
        'NET covert channel: icmp data payload\n1400B ICMP echo with encrypted payload\nExfil tunnel — rate limited + logged',
        'NET traffic analysis: timing correlation\nPacket intervals match known C2 pattern\nEnforcement correlation active',
        'NET rate limit exceeded: 10.44.7.3\n1000 req/s threshold breached\nConnection throttled — 429 responses',
        'NET reflection attack: DNS amplification\n0.1Mbps sent → 4.4Gbps reflected\nUpstream nullroute applied',
        'SEC hypervisor escape attempt\nCVE-2026-XXXX: VM escape via MMIO\nHypervisor patched — vm restarted',
        'SEC container escape: pid=9144\nCgroup namespace breakout detected\nContainer isolated — host processes safe',
        'SEC bootkit: MBR sector 0 modified\nExpected hash a1b2c3 got ff0099\nBootloader compromised — reinstall',
        'SEC firmware tamper: bci_daemon flash\nFlash region 0x10000 checksum mismatch\nFirmware rollback to v3.2 — audit open',
        'SEC golden ticket: kerberos forged\nTGT for uid=0 valid 10 years\nKDC secret compromised — rotate immediately',
        'CORP zero-day CVE-2026-0041: active\nRemote code exec in net_daemon v4.1\nPatch deploying — interim firewall rule',
        'CORP ransomware: 128 files encrypted\nExtension .glock appended — known strain\nIsolate host — restore from offline backup',
        'CORP apt beacon: C2 to 198.51.100.44\nBeacon interval 300s — Cobalt Strike?\nC2 blocked — hunt for lateral movement',
        'CORP data destruction: bci_logs wiped\n/var/log/bci/*.log deleted at 03:22\nAnti-forensic activity — incident open',
        'CORP insider threat: 0x3301 screenshot\nScreen capture software installed covertly\nDLP alert — session terminated',
        'ARC cold storage fail: bucket unavailable\nS3 endpoint timeout after 30s\nArchival suspended — retry in 15m',
        'ARC manifest verify fail: entry 88\nSHA-256 expected a1b2 got c3d4\nManifest tampered — restore from replica',
        'ARC expiry purge fail: permission denied\nProcess uid=1001 cannot delete uid=0 files\nSudo required — manual intervention',
        'ARC dedup collision: hash a1b2c3d4\nTwo distinct 4K blocks with same hash\nSHA-256 collision? — verify algorithm',
        'ARC stream abort: connection reset mid-xfer\nReceived 2.1MB of expected 4.4MB\nPartial archive — resume required',
        'JIT profile corrupt: hotspot table\nProfile header magic invalid 0xFF00\nProfile discarded — cold compilation mode',
        'JIT osr entry fail: backedge at +0x88\nOSR not possible — frame layout mismatch\nFalling back to baseline interpreter',
        'JIT regalloc spill overflow: 64 spill slots\nRegister pressure too high\nCompilation aborted — no space on stack',
        'JIT constant fold loop: infinite iteration\nFolder detected cycle in const graph\nCompilation aborted — graph corrupt',
        'JIT branch prediction miss 88%: hot loop\nBranch pattern non-predictable\nPGO data insufficient — retrain profile',
        'IPC futex contention: 1200 waiters\nbci_lock contended by 1200 threads\nSystem latency 800ms — livelock risk',
        'IPC condvar spurious wakeup storm\n4000 spurious wakeups in 10s\nCondition variable misuse — busy loop',
        'IPC spinlock CPU burn: cpu0 100%\nSpinning for 400ms — no progress\nDeadlock suspected — NMI triggered',
        'IPC rwlock writer starvation\nWriter blocked by continuous readers\n88 seconds without write — livelock',
        'IPC barrier timeout: 8 of 16 arrived\nRemaining 8 threads hung — crashed?\nBarrier abandoned — results invalid',
        'SCHED SIGXCPU: bci_daemon 300s CPU\nProcess consumed 300s wall CPU\nKilled — CPU quota exceeded',
        'SCHED workqueue flood: 65536 pending\nBCI event workqueue overflow\nDropping oldest events — data loss',
        'SCHED kthread stall: bci_flusher\nKernel thread blocked 120s\nForce stopped — filesystem at risk',
        'SCHED irq storm: irq 44 cpu0 100%\nBCI ADC interrupt rate 2MHz\nDriver rate limiting broken — system hang',
        'SCHED preemption disabled too long: 88ms\nbci_spinlock held across schedule\nLatency anomaly — fix critical section',
        'MEM kasan: out-of-bounds in epoch_buf\nWrite at offset 4096 of 4096B obj\nLast byte overwrite — off-by-one',
        'MEM oom: bci_feature_cache 1.9GB\nCache not bounded — unbounded growth\nOOM kill at 1.9GB — bound cache',
        'MEM huge page alloc fail: ENOMEM\nHugeTLB pool exhausted 0 pages free\nFalling back to 4K pages — TLB pressure',
        'MEM pkey fault: key=2 write at 0x5582c000\nProtection key violation on write\nProcess terminated — exploit attempt?',
        'MEM stack smash: bci_classify frame\nCanary 0x12345678 overwritten with 0x41414141\nReturn address corrupted — exploit?',
        'SEC dcsync: ldap query for krbtgt\nOperator 0x3301 querying AD password hashes\nDomain controller alert — contain',
        'SEC pass-the-hash: ntlm auth from 3301\nHash reuse from compromised credential\nLateral movement blocked — rotate',
        'SEC kerberoast: SPN enum by 0x7712\n12 service tickets requested in 3s\nOffline cracking suspected — rotate SPNs',
        'SEC process injection: pid=8812→9144\nWriteProcessMemory + CreateRemoteThread\nCode injected — isolate both processes',
        'SEC credential dump: lsass accessed\nOpenProcess + ReadProcessMemory on lsass\nPass-the-hash risk — rotate all creds',
        'KERN page fault #14 in bci_classify()\nCR2: 0x00000000000000b0 — null deref\nRIP: bci_classify+0x3a4 CS: 0x10',
        'KERN machine check: uncorrectable mem\nMCI_STATUS: 0xbe00000000000800\nMemory offline — degraded DRAM ECC',
        'KERN IRQ 44 affinity storm\nAll CPUs handling bci ADC interrupts\nAffinity pinning broken — rebalance',
        'KERN soft lockup: CPU1 22s\nTask bci_classify not scheduling\nWatchdog fired — process killed',
        'KERN IOMMU fault: dev 0:1b.0\nDMA to address 0x00000000 — null\nbci ADC driver null pointer bug',
        'FS ext4 error: block group 12 bitmap\nBad block count — filesystem inconsistent\numount and fsck required',
        'FS journal commit blocked: EIO\n/dev/sdb1 sector 441200 write error\nJournal stalled — filesystem readonly',
        'FS inode leak: 4412 inodes unclaimed\nOpen but unlinked files not closed\nFD leak in bci_session — close on exit',
        'FS xattr corrupt: security.capability\nCapability xattr decode fail\nProcess drops capabilities — broken env',
        'FS sync timeout: 30s dirty pages\nFlusher blocked on device write\nKernel may force reboot — alert ops',
        'HKB emergency stop: temp 94C\nThermal sensor trip at 90C threshold\nWeapon cold-locked — 600s cooldown',
        'HKB spring fail: resonance +18Hz shift\nFrequency analyzer: spring 3 coil break\nImpulse compensation 60% degraded',
        'HKB piston stall: current 4.4A\nMotor controller fault code 0xE3\nManual extraction required — do not fire',
        'HKB fluid sensor: 0mL remaining\nReservoir empty — 3 cycles since warning\nHydraulic mode disabled — spring only',
        'HKB actuator fault: code 0xA7\nActuator controller CAN bus timeout\nPiston position unknown — emergency stop',
        'RBS selector corrupt: position=2 undefined\nExpected 0 (alpha) or 1 (beta)\nDisc in undefined position — abort',
        'RBS thermal trip: 78C bearing temp\nLubricant breakdown — friction 3x\nMotor current 4.1A — stall imminent',
        'RBS jam: foreign object detected\nFeed mechanism blocked at position 3\nClear procedure: release pin + extract',
        'RBS misfire: primer struck, no discharge\nRound seated but no pressure wave\nDud round — extract and inspect',
        'RBS discharge blocked: safety override\nSafety engaged remotely by corp policy\nDischarge disallowed in zone B',
        'DB orphan edges: 412 unreachable nodes\nGarbage collect failed — nodes pinned\nManual vacuum required — db.Sweep()',
        'DB query timeout: 44s on graph traverse\nPath query depth=12 nodes=9871\nQuery plan chose full scan — index hint',
        'DB unique violation: bci_event.epoch_id\nEpoch 4412 already inserted\nDuplicate insert rejected — idempotency bug',
        'DB graph path fail: node 0x3301 missing\nNode deleted but edges remain\nReferential integrity broken',
        'DB stats stale: 180s since last update\nQuery planner using 6h old statistics\nForce ANALYZE — plans suboptimal',
        // 30 more
        'Neural bridge dropout: ring4 lost contact\nEEG amplitude < noise floor 18ms\nBCI handshake reset — reclassify',
        'BCI handshake refused: key 0xBE04 revoked\nVault reports key expired 48h ago\nOperator locked out — manual unlock',
        'RBS spring shatter: resonance sensor 0x3A\nHigh-speed impact fragment at 38ms\nDisc rotation halted — catastrophic fail',
        'HKB valve blowout: pressure 140 PSI\nSeat gasket failed — rated 110 PSI\nVent to atmosphere — gas leak active',
        'GLMZ gate 0x22 auth fail uid=0x4492\nCert revoked by enforcement order 9412\nAll routes via gate blocked — reroute',
        'Relay blackhole: dist7→dist12 loop\nTTL expired 3 hops — routing cycle\nPacket black-holed — trace route',
        'Key store corrupt: sector 0x44A2\nHMAC mismatch on master record\nKey material unreadable — vault sealed',
        'DB redo log corrupt: frame 8192\nChecksum 0xDEAD != expected 0x3AF7\nRecovery impossible — restore from backup',
        'VM page table fault: cr3=0x0044 rip=0x00\nKernel null-pointer deref in vm_alloc\nSystem halted — memory model broken',
        'JIT segment overflow: code cache full\nCache 4MB — eviction threshold hit 100%\nHot functions deoptimised — degraded',
        'IPC ring overflow: consumers stalled\nProducer pushed 4096 unread messages\nRing sealed — bci_daemon blocked',
        'TLS session bomb: 0 bytes handshake\nClient sent RST mid-ClientHello\nMitM or scan probe — block IP',
        'BCI flood attack: 14kpps to ring buffer\nMalformed ADC frames from addr 0xBE04\nBuffer overflow risk — drop source',
        'RBS cam desync: encoder mismatch 0x22\nEncoder A and B disagree by 3 ticks\nPosition unknown — emergency stop',
        'HKB rack jam: gear 3 stall at 4ms\nFeed mechanism seized mid-stroke\nDo not fire — mechanical hazard',
        'GLMZ perimeter breach: dist4 wall C\nForce sensor 0x3301 tripped, 0xBE04 quiet\nEnforcement notified — 90s response ETA',
        'Corp exfil volume: 48GB to 203.0.113.9\nDestination corp blacklist match rate 94%\nDLP blocked session — forensic queued',
        'SEC replay storm: 8800 tokens in 30s\nSame nonce 0x3301 repeated 4400×\nSession revoked — incident #8812',
        'DB WAL storm: 4GB redo in 22s\nWrite rate 200MB/s — disk saturated\nCheckpoint blocked — db frozen',
        'ARP poison: gateway 10.44.0.1 spoofed\nMAC 0:0:0:9f:3a:01 claims GW address\nTraffic intercepted — network isolated',
        'VM OOM cascade: 8 processes SIGKILL\nFree pages: 0 — kernel killing tasks\nSystem degraded — restart required',
        'JIT cache corrupt: hash mismatch fn=0x44\nExpected bytes [90 90 90] got [cc cc cc]\nExecution halted — possible patch',
        'IPC deadlock global: all queues stalled\nbci_daemon, rbs_ctrl, hkb_mon all waiting\nSystem heartbeat timeout — restart',
        'Archive block missing: idx entry 0x3AF7\nBlock referenced by 3 nodes, file deleted\nIntegrity broken — rebuild index',
        'Slab poison detect: kmem cache bci_buf\nPoison pattern 0x6b overwritten\nuse-after-free confirmed — halt',
        'HPET timer fault: counter frozen 44ms\nHardware timer register read returns 0\nSystem timekeeping degraded',
        'CPUID mismatch: hypervisor flag set\nNative boot expected — VM detected\nEnclave attestation invalidated — abort',
        'Auth module unresponsive: 10s timeout\nCrest auth IPC no reply to 8 queries\nWeapon auth state unknown — safe mode',
        'District feed blackout: dist4→GLMZ\nNo telemetry for 180s — correlation lost\nBlind spot in enforcement coverage',
        'BCI classifier poison inject\nAdversarial signal injected via ring2\nClassifier predicting adversary labels',
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
        // ── 10 additional fragments ────────────────────────────────────────
        'while (raft.Role == RaftRole.Candidate) {\n    var votes = 1;\n    foreach (var peer in cluster.Peers) {\n        var resp = await peer.RequestVoteAsync(\n            term, raft.LogIndex, raft.LogTerm,\n            cts.Token);\n        if (resp.VoteGranted) votes++;\n        if (resp.Term > term) {\n            raft.StepDown(resp.Term);\n            break;\n        }\n    }\n    if (votes > cluster.Peers.Count / 2) {\n        raft.PromoteToLeader();\n        log.Info($"leader elected term={term}");\n        break;\n    }\n    await Task.Delay(rng.Next(150, 300));\n}',
        'foreach (var span in trace.Spans\n    .Where(s => s.Duration > SLOW_THRESHOLD)\n    .OrderByDescending(s => s.Duration)) {\n    log.Warn(\n        $"slow span {span.OperationName} " +\n        $"duration={span.Duration.TotalMilliseconds}ms");\n    foreach (var tag in span.Tags) {\n        log.Debug($"  {tag.Key}={tag.Value}");\n    }\n    if (span.Duration > KILL_THRESHOLD) {\n        await alerts.PageOnCallAsync(\n            span, AlertSeverity.High);\n        break;\n    }\n}',
        'if (queue.Count > BACKPRESSURE_HIGH) {\n    if (!backpressure.IsActive) {\n        backpressure.Engage();\n        log.Warn("backpressure engaged");\n    }\n    await Task.Delay(\n        Math.Min(queue.Count, MAX_BP_DELAY));\n} else if (queue.Count < BACKPRESSURE_LOW &&\n           backpressure.IsActive) {\n    backpressure.Release();\n    log.Info("backpressure released");\n}\nfor (int i = 0; i < BATCH_SIZE && queue.TryDequeue(out var item); i++) {\n    await sink.WriteAsync(item, cts.Token);\n}',
        'var ms = JsonSerializer.Deserialize<MerkleSnapshot>(blob);\nfor (int level = 0; level < ms.Levels.Length; level++) {\n    var pairs = ms.Levels[level];\n    for (int i = 0; i < pairs.Length; i += 2) {\n        var combined = sha256.ComputeHash(\n            pairs[i].Concat(\n                i + 1 < pairs.Length\n                    ? pairs[i+1]\n                    : pairs[i]).ToArray());\n        if (level + 1 < ms.Levels.Length &&\n            !combined.SequenceEqual(\n                ms.Levels[level+1][i/2])) {\n            throw new MerkleProofException(\n                $"hash mismatch L{level}");\n        }\n    }\n}',
        'await using var conn = await pool.AcquireAsync(cts.Token);\nawait using var tx = await conn.BeginTransactionAsync(\n    IsolationLevel.Serializable, cts.Token);\ntry {\n    foreach (var op in batch) {\n        await op.ApplyAsync(conn, cts.Token);\n        if (cts.IsCancellationRequested) break;\n    }\n    if (await conflictDetector.HasConflictsAsync(conn)) {\n        await tx.RollbackAsync(cts.Token);\n        throw new SerializationException("write skew");\n    }\n    await tx.CommitAsync(cts.Token);\n    metrics.TxCommitted++;\n} catch (Exception ex) {\n    await tx.RollbackAsync(cts.Token);\n    metrics.TxRolledBack++;\n    throw;\n}',
        'for (int z = 0; z < volume.D; z++) {\n    for (int y = 0; y < volume.H; y++) {\n        for (int x = 0; x < volume.W; x++) {\n            var v = volume[x, y, z];\n            if (v < ISO_THRESHOLD) continue;\n            var n = ComputeNormal(volume, x, y, z);\n            var col = palette.Sample(\n                Vector3.Dot(n, lightDir));\n            framebuffer.Plot(x, y, col,\n                ISO_THRESHOLD - v + DEPTH_BIAS * z);\n        }\n    }\n    if (z % SCANLINE_REPORT == 0)\n        progress?.Report((float)z / volume.D);\n}',
        'using var batch = bus.CreateBatch();\nforeach (var ev in pending) {\n    if (!batch.TryAdd(ev.Serialize())) {\n        await bus.PublishAsync(batch, cts.Token);\n        batch.Reset();\n        if (!batch.TryAdd(ev.Serialize())) {\n            log.Error(\n                $"event {ev.Id} too large — dropped");\n            metrics.OversizedDropped++;\n            continue;\n        }\n    }\n}\nif (batch.Count > 0) {\n    await bus.PublishAsync(batch, cts.Token);\n    metrics.BatchedPublish += batch.Count;\n}',
        'while (model.GradientNorm > CONVERGE_TOL &&\n       step < MAX_STEPS) {\n    var grad = await loss.BackwardAsync(\n        model, batch, cts.Token);\n    if (clipNorm > 0)\n        grad.ClipByNorm(clipNorm);\n    model.Apply(grad, lr);\n    if (step % EVAL_INTERVAL == 0) {\n        var valLoss = await Evaluate(model, valSet);\n        if (valLoss > prevValLoss + EARLY_STOP_DELTA)\n            patience--;\n        else { prevValLoss = valLoss; patience = MAX_PATIENCE; }\n        if (patience <= 0) {\n            log.Info($"early stop step={step}");\n            break;\n        }\n    }\n    step++;\n}',
        'var holds = vault.AcquireMany(\n    keys.Select(k => new HoldSpec(\n        k, HoldKind.Exclusive, HOLD_TIMEOUT))\n        .ToList());\nif (holds.Any(h => !h.Acquired)) {\n    var miss = holds.First(h => !h.Acquired);\n    log.Warn($"hold denied: {miss.Key} — {miss.Reason}");\n    await Task.WhenAll(holds.Where(h => h.Acquired)\n        .Select(h => h.ReleaseAsync()));\n    throw new HoldContentionException(miss.Key);\n}\ntry {\n    foreach (var h in holds)\n        await mutator.ApplyAsync(h.Resource);\n} finally {\n    foreach (var h in holds) await h.ReleaseAsync();\n}',
        'foreach (var bucket in shard.Buckets\n    .OrderByDescending(b => b.AccessCount)) {\n    if (bucket.HotKeys.Count == 0) continue;\n    var promote = bucket.HotKeys\n        .Where(k => k.Hits >= PROMOTE_THRESHOLD)\n        .Take(PROMOTE_BATCH).ToList();\n    foreach (var k in promote) {\n        try {\n            cache.L1.Insert(k.Id, k.Value);\n            bucket.HotKeys.Remove(k);\n            metrics.Promoted++;\n        } catch (CacheFullException) {\n            cache.L1.Evict(EVICT_FRACTION);\n            log.Debug($"L1 evict @{cache.L1.UsedB}B");\n            break;\n        }\n    }\n}',
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
        // 20 more
        'for (int i = 0; i < relays.Count; i++) {\n    if (!relays[i].IsReachable) {\n        skipped++; continue;\n    }\n    var rtt = await relays[i].PingAsync(cts.Token);\n    if (rtt > SLA_RTT_MS) {\n        slow.Add(relays[i]);\n        log.Warn(\n            $"relay {relays[i].Id} rtt={rtt}ms > SLA");\n        continue;\n    }\n    if (rtt < bestRtt) {\n        bestRtt   = rtt;\n        preferred = relays[i];\n    }\n}\nif (preferred == null)\n    throw new NoRelayException(skipped);',
        'while (!bci.IsLocked) {\n    var raw = cortex.Sample(SAMPLE_BURST);\n    bim.Feed(raw);\n    lock_attempts++;\n    if (lock_attempts > MAX_LOCK_ATTEMPTS) {\n        log.Fatal(\n            "bci lock attempts exceeded");\n        throw new BciLockoutException();\n    }\n    if (bim.Confidence < MIN_CONFIDENCE) {\n        await Task.Delay(RETRY_DELAY_MS);\n        continue;\n    }\n    if (!bim.ValidateKey(neuralKey)) {\n        sec.Alert(SecEvent.KeyMismatch);\n        await Task.Delay(KEY_RETRY_MS * lock_attempts);\n        continue;\n    }\n    bci.Lock();\n}',
        'foreach (var district in world.Districts\n    .Where(d => d.HasActiveRelay)\n    .OrderBy(d => d.Latency)) {\n    try {\n        var route = await router.FindAsync(\n            source, district.GlmzGate,\n            RouteFlags.DarkNodeOk);\n        if (route.HasBlacklist) {\n            log.Warn(\n                $"route via {district.Id} blacklisted");\n            continue;\n        }\n        await route.OpenAsync(cts.Token);\n        connections[district.Id] = route;\n    } catch (EnforcementBlockException ex) {\n        log.Error(\n            $"blocked at {ex.Gate}: {ex.Reason}");\n        blockedDistricts.Add(district.Id);\n    }\n}',
        'Task.Run(async () => {\n    var window = new SlidingWindow<float>(\n        WINDOW_SIZE);\n    while (!cts.IsCancellationRequested) {\n        var sample = bci.SampleRaw();\n        window.Add(sample);\n        if (window.IsFull) {\n            float mean = window.Mean();\n            float std  = window.StdDev();\n            float z    = Math.Abs(\n                (sample - mean) / std);\n            if (z > ANOMALY_Z) {\n                log.Warn(\n                    $"bci anomaly z={z:F2} at {sample:F4}");\n                anomalies.Record(DateTime.UtcNow, z);\n            }\n        }\n        await Task.Delay(SAMPLE_INTERVAL_MS);\n    }\n});',
        'for (int slot = 0; slot < rbs.SlotCount; slot++) {\n    var pos = rbs.ReadEncoder(slot);\n    if (!rbs.IsValidPosition(pos)) {\n        faults.Add(new EncoderFault {\n            Slot     = slot,\n            Position = pos,\n            Expected = rbs.NominalPosition(slot)\n        });\n        log.Error(\n            $"rbs encoder fault slot={slot} pos={pos}");\n        if (faults.Count >= RBS_FAULT_LIMIT) {\n            weapon.SafetyEngage();\n            throw new RbsEncoderException(faults);\n        }\n        continue;\n    }\n    rbs.Calibrate(slot, pos);\n}',
        'while (vault.IsSealed) {\n    var shards = await keyStore.LoadShardsAsync(\n        vault.Id, cts.Token);\n    int valid = 0;\n    for (int i = 0; i < shards.Count; i++) {\n        if (shards[i].VerifyHmac(masterKey)) {\n            valid++;\n            xorBuf.XorWith(shards[i].Material);\n        } else {\n            log.Warn(\n                $"shard {i} hmac invalid — skip");\n        }\n    }\n    if (valid >= vault.Threshold) {\n        var key = kdf.Derive(\n            xorBuf, vault.Salt, KDF_ITERS);\n        vault.Unseal(key);\n        log.Info(\n            $"vault unsealed {valid}/{shards.Count}");\n    } else {\n        throw new VaultSealException(valid);\n    }\n}',
        'foreach (var op in pendingOps\n    .Where(o => o.IsExpired(DateTime.UtcNow))\n    .ToList()) {\n    log.Warn(\n        $"op {op.Id} expired after {op.Age}ms");\n    switch (op.Type) {\n        case OpType.Write:\n            await journal.AbortAsync(op.TxnId);\n            break;\n        case OpType.Read:\n            op.Fail(new TimeoutException());\n            break;\n        case OpType.Flush:\n            if (!fs.IsFlushing)\n                await fs.FlushAsync();\n            break;\n    }\n    pendingOps.Remove(op);\n    expired++;\n}',
        'Task.Run(async () => {\n    using var watcher = new MemoryWatcher(\n        WATCH_INTERVAL_MS);\n    await foreach (var snap in watcher\n        .StreamAsync(cts.Token)) {\n        var delta = snap.Rss - prevRss;\n        if (delta > LEAK_THRESHOLD_KB * 1024) {\n            leakSamples++;\n            log.Warn(\n                $"rss growth +{delta / 1024}KB sample={leakSamples}");\n            if (leakSamples >= LEAK_CONFIRM_SAMPLES) {\n                await alerting.FireAsync(\n                    Alert.MemoryLeak, snap.Rss);\n                leakSamples = 0;\n            }\n        } else {\n            leakSamples = 0;\n        }\n        prevRss = snap.Rss;\n    }\n});',
        'for (int r = startRow; r < endRow; r++) {\n    var page = db.FetchPage(r / PAGE_ROWS);\n    var rec  = page.GetRow(r % PAGE_ROWS);\n    if (rec.IsDeleted) continue;\n    if (!predicate(rec)) continue;\n    if (rec.Version > snapshotLsn) {\n        var old = mvcc.FindVersion(\n            rec.RowId, snapshotLsn);\n        if (old == null) continue;\n        rec = old;\n    }\n    projection.Project(rec, out var row);\n    yield return row;\n    if (++count >= FETCH_LIMIT) {\n        log.Debug(\n            $"scan limit {FETCH_LIMIT} hit at row {r}");\n        yield break;\n    }\n}',
        'while (tls.HandshakeState != TlsState.Done) {\n    var msg = await net.RecvAsync(cts.Token);\n    switch (tls.HandshakeState) {\n        case TlsState.ClientHello:\n            tls.ProcessClientHello(msg);\n            await net.SendAsync(\n                tls.BuildServerHello());\n            await net.SendAsync(\n                tls.BuildCertificate());\n            break;\n        case TlsState.KeyExchange:\n            tls.ProcessKeyExchange(msg);\n            sessionKey = tls.DeriveKeys();\n            await net.SendAsync(\n                tls.BuildFinished());\n            break;\n        case TlsState.Finished:\n            tls.VerifyFinished(msg);\n            break;\n        default:\n            throw new TlsProtocolException(\n                tls.HandshakeState);\n    }\n}',
        'for (int epoch = 0; epoch < MAX_EPOCHS; epoch++) {\n    float loss = 0f;\n    for (int b = 0; b < batches.Count; b++) {\n        var (x, y) = batches[b];\n        var pred = model.Forward(x);\n        var l    = loss_fn(pred, y);\n        loss    += l;\n        model.Backward(l);\n        if ((b + 1) % ACCUM_STEPS == 0) {\n            optimizer.Step(lr);\n            optimizer.ZeroGrad();\n        }\n    }\n    loss /= batches.Count;\n    log.Info(\n        $"epoch {epoch}: loss={loss:F4} lr={lr:F6}");\n    if (loss < CONVERGE_THRESHOLD) {\n        log.Info("converged");\n        break;\n    }\n    lr *= LR_DECAY;\n}',
        'Task.Run(async () => {\n    var conn = await glmz.ConnectAsync(\n        GlmzNode.DarkNode, cts.Token);\n    using var enc = new FrameEncryptor(\n        sessionKey, conn);\n    while (!cts.IsCancellationRequested) {\n        var payload = await queue.DequeueAsync(\n            cts.Token);\n        var frame = new DarkFrame {\n            Nonce   = rng.NextBytes(12),\n            Payload = payload,\n            Stamp   = DateTime.UtcNow\n        };\n        await enc.SendAsync(frame);\n        metrics.DarkSent++;\n        if (metrics.DarkSent % LOG_INTERVAL == 0) {\n            log.Debug(\n                $"dark node: {metrics.DarkSent} frames");\n        }\n    }\n});',
        'foreach (var node in graph.GetNeighbors(target)\n    .Where(n => n.District == operatorDistrict)\n    .OrderByDescending(n => n.TrustScore)) {\n    if (enforcement.HasWarrant(node.Id)) {\n        log.Warn(\n            $"skip node {node.Id} — warrant active");\n        continue;\n    }\n    var cred = await auth.IssueCredsAsync(\n        node.Id, scope, TTL_HOURS);\n    if (!cred.IsValid) {\n        log.Error(\n            $"cred issue failed: {cred.Reason}");\n        continue;\n    }\n    grants.Add(node.Id, cred);\n    if (grants.Count >= MAX_GRANTS) break;\n}',
        'while (compaction.HasWork) {\n    var run = compaction.NextRun();\n    using var merger = new SstMerger(\n        run.Inputs, run.Output);\n    int written = 0;\n    while (merger.MoveNext()) {\n        var kv = merger.Current;\n        if (kv.IsTombstone &&\n            kv.Seq < compaction.GcHorizon) {\n            continue;\n        }\n        run.Output.Write(kv);\n        written++;\n        if (written % COMPACT_LOG_INTERVAL == 0) {\n            log.Debug(\n                $"compact: {written} kv written");\n        }\n    }\n    compaction.Complete(run);\n    log.Info(\n        $"compact done: {written} kv in {run.Inputs.Count} files");\n}',
        'Task.Run(async () => {\n    var dial  = new Dialer(relayPool);\n    var retry = new ExponentialBackoff(\n        BASE_MS, MAX_MS, JITTER_FACTOR);\n    int attempts = 0;\n    while (!cts.IsCancellationRequested) {\n        try {\n            var conn = await dial.DialAsync(\n                dist12.GlmzEndpoint, cts.Token);\n            log.Info(\n                $"connected after {attempts} attempts");\n            await conn.ServeAsync(handler, cts.Token);\n            retry.Reset();\n            attempts = 0;\n        } catch (OperationCanceledException) {\n            break;\n        } catch (Exception ex) {\n            attempts++;\n            var delay = retry.Next();\n            log.Warn(\n                $"dial fail #{attempts}: {ex.Message} — retry in {delay}ms");\n            await Task.Delay(delay, cts.Token);\n        }\n    }\n});',
        'for (int i = 0; i < ops.Count; i++) {\n    var op = ops[i];\n    if (!authorization.Check(\n            op.Uid, op.Resource, op.Action)) {\n        audit.Record(AuditEvent.Denied,\n            op.Uid, op.Resource, op.Action);\n        denied.Add(i);\n        continue;\n    }\n    try {\n        var result = await executor.RunAsync(\n            op, cts.Token);\n        results[i] = result;\n        audit.Record(AuditEvent.Allowed,\n            op.Uid, op.Resource, op.Action);\n    } catch (PolicyException ex) {\n        audit.Record(AuditEvent.PolicyViolation,\n            op.Uid, ex.Rule);\n        results[i] = Result.Deny(ex.Rule);\n    }\n}',
        'while (recv.HasData) {\n    var seg = recv.ReadSegment();\n    if (seg.Seq != expected) {\n        outOfOrder.Enqueue(seg);\n        if (outOfOrder.Count > OOO_LIMIT) {\n            log.Error("ooo buffer overflow — reset");\n            recv.SendReset();\n            recv.Clear();\n            outOfOrder.Clear();\n            expected = 0;\n            break;\n        }\n        continue;\n    }\n    reassembly.Write(seg.Data);\n    expected = seg.Seq + seg.Data.Length;\n    while (outOfOrder.TryPeek(out var next)\n        && next.Seq == expected) {\n        outOfOrder.Dequeue();\n        reassembly.Write(next.Data);\n        expected = next.Seq + next.Data.Length;\n    }\n}',
        'foreach (var alert in monitor.GetPending()\n    .OrderByDescending(a => a.Severity)\n    .Take(MAX_ALERTS_PER_TICK)) {\n    switch (alert.Severity) {\n        case Severity.Critical:\n            await pager.PageAsync(\n                oncall.Primary, alert);\n            await pager.PageAsync(\n                oncall.Secondary, alert);\n            break;\n        case Severity.High:\n            await pager.PageAsync(\n                oncall.Primary, alert);\n            break;\n        case Severity.Medium:\n            await email.SendAsync(\n                team.Email, alert);\n            break;\n        default:\n            await slack.PostAsync(\n                CHANNEL_OPS, alert);\n            break;\n    }\n    monitor.Acknowledge(alert.Id);\n}',
        'Task.Run(async () => {\n    var hb = new HeartbeatMonitor(\n        HEARTBEAT_INTERVAL_MS,\n        HEARTBEAT_TIMEOUT_MS);\n    hb.OnTimeout += async (nodeId) => {\n        log.Error(\n            $"node {nodeId} heartbeat timeout");\n        var node = cluster.Get(nodeId);\n        if (node?.IsPrimary == true) {\n            await election.StartAsync();\n        } else {\n            await cluster.MarkDeadAsync(nodeId);\n            await rebalancer.RebalanceAsync();\n        }\n    };\n    await hb.RunAsync(cts.Token);\n});',
        'for (int pass = 0; pass < 3; pass++) {\n    switch (pass) {\n        case 0:\n            foreach (var n in graph.Nodes)\n                n.Color = Color.White;\n            break;\n        case 1:\n            foreach (var n in graph.Nodes\n                .Where(n => n.Color == Color.White)) {\n                dfs.Visit(n);\n            }\n            break;\n        case 2:\n            var scc = new List<Component>();\n            foreach (var n in dfs.FinishOrder) {\n                if (n.Component == null) {\n                    var c = dfs2.Explore(n);\n                    scc.Add(c);\n                }\n            }\n            graph.Components = scc;\n            break;\n    }\n}',
        'while (true) {\n    var acquired = await sem.WaitAsync(\n        ACQUIRE_TIMEOUT_MS);\n    if (!acquired) {\n        log.Warn(\n            $"lock timeout after {ACQUIRE_TIMEOUT_MS}ms");\n        contention++;\n        if (contention > DEADLOCK_THRESHOLD) {\n            log.Fatal("possible deadlock — abort");\n            throw new DeadlockException();\n        }\n        await Task.Delay(\n            BACKOFF_BASE_MS * contention);\n        continue;\n    }\n    try {\n        return await criticalSection();\n    } finally {\n        contention = 0;\n        sem.Release();\n    }\n}',
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
        'for (int i = 0; i < db.NodeCount; i++) {\n    var node = db.ReadNode(i);\n    if (!node.Tags.Any()) continue;\n    var edges = db.GetEdges(node.Id);\n    var score = edges\n        .Where(e => e.Type == EdgeType.Trust)\n        .Sum(e => e.Weight);\n    node.TrustScore = score /\n        Math.Max(1, edges.Count);\n    if (node.TrustScore < TRUST_THRESHOLD) {\n        log.Warn(\n            $"node {node.Id} trust {node.TrustScore:F2}");\n        enforcement.Flag(node.Id);\n    }\n    db.UpdateNode(node);\n}',
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
        // 100 more
        'for (int r = 0; r < CORTEX_RINGS; r++) {\n    var z = cortex.SampleZ(r);\n    if (Math.Abs(z) > Z_SPIKE) {\n        log.Warn($"cortex ring{r} spike z={z:F2}");\n        spikes[r]++;\n        if (spikes[r] > MAX_SPIKES) {\n            cortex.Disable(r);\n            audit.Record(\n                AuditEvent.CortexDisabled, r);\n        }\n        continue;\n    }\n    spikes[r] = Math.Max(0, spikes[r] - 1);\n    baseline[r] = baseline[r] * 0.99f + z * 0.01f;\n}',
        'while (bci.SessionAge < MAX_SESSION_AGE) {\n    var raw = cortex.ReadFrame();\n    if (raw.SnrDb < MIN_SNR_DB) {\n        log.Warn($"cortex snr {raw.SnrDb:F1}dB low");\n        await Task.Delay(SNR_RECOVER_MS);\n        continue;\n    }\n    var spec = fft.Compute(raw, FFT_WINDOW);\n    powerAlpha = spec.BandPower(8, 13);\n    powerBeta  = spec.BandPower(13, 30);\n    if (powerBeta / powerAlpha > BETA_DOMINANT) {\n        focus.Promote();\n    } else if (powerAlpha > REST_THRESHOLD) {\n        focus.Demote();\n    }\n    await bci.PostFrameAsync(raw);\n}',
        'foreach (var sample in cortex\n    .StreamSliding(WINDOW_MS, STEP_MS,\n                   cts.Token)) {\n    if (artifact.Detect(sample)) {\n        artifactsInWindow++;\n        continue;\n    }\n    if (artifactsInWindow > ARTIFACT_REJECT) {\n        log.Warn(\n            $"window dropped: {artifactsInWindow} artifacts");\n        artifactsInWindow = 0;\n        continue;\n    }\n    var feat = extractor.Extract(sample);\n    var intent = bim.Classify(feat);\n    history.Add(intent);\n    artifactsInWindow = 0;\n}',
        'Task.Run(async () => {\n    while (cortex.IsLinked) {\n        var probe = cortex.ProbeImpedance();\n        for (int r = 0; r < probe.Length; r++) {\n            if (probe[r] > Z_DEGRADED_KOHM) {\n                log.Warn(\n                    $"ring{r} z={probe[r]}kΩ degrading");\n                gel.RequestRefill(r);\n            }\n        }\n        await Task.Delay(\n            IMPEDANCE_INTERVAL_MS, cts.Token);\n    }\n});',
        'for (int e = 0; e < CAL_EPOCHS; e++) {\n    var trials = await operator2\n        .CollectTrialsAsync(TRIALS_PER_EPOCH,\n                            cts.Token);\n    bim.Train(trials);\n    var acc = bim.Validate(holdout);\n    log.Info(\n        $"cal epoch {e}: acc={acc:P1}");\n    if (acc >= TARGET_CAL_ACC) {\n        bim.Commit();\n        log.Info("cal converged");\n        break;\n    }\n    if (e == CAL_EPOCHS - 1) {\n        log.Warn("cal incomplete — partial commit");\n        bim.PartialCommit();\n    }\n}',
        'while (intent.IsTransitioning) {\n    var next = bim.PeekIntent();\n    if (next == lastIntent) {\n        stableTicks++;\n        if (stableTicks > STABILIZE_TICKS) {\n            bci.Commit(next);\n            intent.MarkStable();\n            break;\n        }\n    } else {\n        stableTicks = 0;\n        lastIntent = next;\n    }\n    await Task.Delay(HYSTERESIS_MS);\n}',
        'foreach (var spike in cortex\n    .DetectSpikes(THRESHOLD_UV,\n                  REFRACTORY_MS)) {\n    spikeTrain.Add(spike);\n    if (spikeTrain.Count > MAX_TRAIN_LEN) {\n        spikeTrain.RemoveAt(0);\n    }\n    var rate = spikeTrain.Count /\n        (RATE_WINDOW_MS / 1000f);\n    if (rate > BURST_RATE_HZ) {\n        log.Warn(\n            $"cortex burst {rate:F1}Hz @{spike.Channel}");\n        burstDetector.Record(spike);\n    }\n}',
        'for (int r = 0; r < CONTACT_RINGS; r++) {\n    var polA = adc.SamplePolarity(r, Polarity.A);\n    var polB = adc.SamplePolarity(r, Polarity.B);\n    if (Math.Sign(polA) == Math.Sign(polB)) {\n        log.Error(\n            $"ring{r} polarity collapse");\n        cortex.Reseat(r);\n        if (!cortex.Verify(r)) {\n            cortex.Disable(r);\n            audit.Record(\n                AuditEvent.RingPolarityFail, r);\n        }\n    }\n}',
        'while (!bci.IsLocked) {\n    try {\n        var key = await neural\n            .DeriveLockKeyAsync(\n                operatorId, sessionSalt,\n                cts.Token);\n        if (bci.TryLock(key)) {\n            log.Info("bci lock established");\n            break;\n        }\n        log.Warn("bci lock rejected — retry");\n    } catch (NeuralDesyncException) {\n        await cortex.ResyncAsync(cts.Token);\n    }\n    await Task.Delay(LOCK_RETRY_MS, cts.Token);\n}',
        'Task.Run(async () => {\n    while (cortex.IsLinked) {\n        var baseline = cortex.MeasureBaseline(\n            BASELINE_SAMPLES);\n        for (int r = 0; r < baseline.Length; r++) {\n            var delta = Math.Abs(\n                baseline[r] - nominal[r]);\n            if (delta > BASELINE_DRIFT) {\n                log.Warn(\n                    $"ring{r} baseline drift {delta:F1}");\n                cortex.Recenter(r);\n            }\n        }\n        await Task.Delay(\n            BASELINE_INTERVAL_MS, cts.Token);\n    }\n});',
        'for (int p = 0; p < RBS_PORTS; p++) {\n    rbs.SelectPort(p);\n    var enc = rbs.ReadEncoder();\n    if (Math.Abs(enc - rbs.Expected(p)) >\n            ENCODER_TOL) {\n        log.Error(\n            $"rbs port{p} encoder off by {enc - rbs.Expected(p)}");\n        rbs.Recalibrate(p);\n        if (!rbs.Verify(p)) {\n            weapon.SafetyEngage();\n            throw new RbsEncoderException(p);\n        }\n    }\n}',
        'while (rbs.IsCycling) {\n    var state = rbs.PollBolt();\n    if (state == BoltState.OutOfBattery &&\n            elapsed > OOB_TIMEOUT_MS) {\n        log.Error(\n            $"bolt OOB for {elapsed}ms");\n        weapon.ClearJam();\n        if (!rbs.Verify(Port.Current)) {\n            weapon.SafetyEngage();\n            throw new BoltStuckException();\n        }\n    }\n    if (state == BoltState.InBattery) {\n        rbs.MarkReady();\n        break;\n    }\n    elapsed += POLL_INTERVAL_MS;\n    await Task.Delay(POLL_INTERVAL_MS);\n}',
        'foreach (var port in rbs.Ports) {\n    var rounds = magazine.ReadCounter(port);\n    if (rounds < LOW_ROUND_THRESHOLD) {\n        log.Warn(\n            $"port {port} rounds {rounds} low");\n        hud.SetIndicator(port, IndicatorState.Low);\n    } else if (rounds == 0) {\n        rbs.LockPort(port);\n        led.Set(port, LedColor.Red);\n    }\n    metrics.RoundsByPort[port] = rounds;\n}',
        'switch (selector.Position) {\n    case SelectorPosition.Safe:\n        weapon.SafetyEngage();\n        led.Set(LedColor.Off);\n        break;\n    case SelectorPosition.Semi:\n        weapon.SetMode(FireMode.Semi);\n        led.Set(LedColor.Green);\n        break;\n    case SelectorPosition.Burst:\n        weapon.SetMode(FireMode.Burst3);\n        led.Set(LedColor.Amber);\n        break;\n    case SelectorPosition.Auto:\n        weapon.SetMode(FireMode.Auto);\n        led.Set(LedColor.Red);\n        break;\n    default:\n        log.Warn(\n            $"selector unknown pos {selector.Position}");\n        weapon.SafetyEngage();\n        break;\n}',
        'while (trigger.IsPressed) {\n    var force = trigger.ReadForce();\n    if (force > TRIGGER_BREAK_FORCE) {\n        if (!weapon.IsSafe) {\n            await weapon.FireAsync();\n            shotsTaken++;\n            if (shotsTaken >= burstLimit) {\n                weapon.HaltBurst();\n                break;\n            }\n        }\n    }\n    if (trigger.HoldMs > MAX_HOLD_MS) {\n        log.Warn(\n            $"trigger held {trigger.HoldMs}ms");\n        weapon.SafetyEngage();\n        break;\n    }\n    await Task.Delay(1);\n}',
        'for (int slot = 0; slot < MAG_SLOTS; slot++) {\n    var mag = magazine.Read(slot);\n    if (mag.IsEmpty) continue;\n    if (mag.Sn != mag.ExpectedSn) {\n        log.Warn(\n            $"mag slot{slot} sn mismatch {mag.Sn:X}");\n        audit.Record(\n            AuditEvent.MagMismatch, slot);\n        magazine.Eject(slot);\n        continue;\n    }\n    inventory[slot] = mag;\n}',
        'Task.Run(async () => {\n    while (weapon.IsArmed) {\n        var temp = suppressor.ReadTemp();\n        if (temp > SUPPRESSOR_CRIT_C) {\n            log.Error(\n                $"suppressor {temp:F0}C — cooldown");\n            weapon.RequestCooldown();\n            await Task.Delay(\n                COOLDOWN_MS, cts.Token);\n        } else if (temp > SUPPRESSOR_WARN_C) {\n            hud.SetIndicator(\n                Hud.SuppressorWarn, true);\n        }\n        await Task.Delay(\n            TEMP_POLL_MS, cts.Token);\n    }\n});',
        'foreach (var actuator in rbs.Actuators) {\n    if (actuator.LubeCycles >=\n            LUBE_INTERVAL) {\n        log.Info(\n            $"actuator {actuator.Id} lube due");\n        await actuator.LubeAsync(\n            LUBE_MICROLITERS);\n        actuator.LubeCycles = 0;\n        audit.Record(\n            AuditEvent.RbsLubed, actuator.Id);\n    }\n    if (actuator.WearScore > WEAR_REPLACE) {\n        log.Error(\n            $"actuator {actuator.Id} wear {actuator.WearScore:F2}");\n        rbs.FlagForReplacement(actuator.Id);\n    }\n}',
        'for (int cycle = 0; cycle < TEST_CYCLES; cycle++) {\n    rbs.LoadSpring(SPRING_LOAD_N);\n    var rebound = rbs.MeasureRebound();\n    if (rebound < REBOUND_MIN) {\n        log.Warn(\n            $"spring rebound {rebound:F1}mm low @ cycle {cycle}");\n        rbs.FlagSpringFatigue();\n        break;\n    }\n    rbs.UnloadSpring();\n    if (cycle % 100 == 0) {\n        log.Debug(\n            $"spring cycle {cycle}: rebound={rebound:F1}mm");\n    }\n}',
        'while (hkb.IsCycling) {\n    var stroke = hkb.SampleStroke();\n    if (stroke.Velocity > VELOCITY_LIMIT) {\n        log.Warn(\n            $"hkb stroke v={stroke.Velocity:F2}m/s high");\n        overspeedCount++;\n        if (overspeedCount > OVERSPEED_TRIP) {\n            hkb.EmergencyVent();\n            throw new HkbOverspeedException();\n        }\n    }\n    hkb.AbsorbEnergy(stroke.KineticJ);\n    metrics.HkbEnergyJ += stroke.KineticJ;\n}',
        'for (int s = 0; s < hkb.SpringStages; s++) {\n    var preload = hkb.MeasurePreload(s);\n    if (preload < PRELOAD_MIN[s] ||\n            preload > PRELOAD_MAX[s]) {\n        log.Warn(\n            $"hkb stage{s} preload {preload}N out of spec");\n        hkb.RequestAdjust(s, NOMINAL_PRELOAD[s]);\n        adjusted++;\n    }\n}\nif (adjusted > MAX_ADJUSTMENTS) {\n    log.Error("hkb preload — service required");\n    audit.Record(AuditEvent.HkbServiceDue);\n}',
        'Task.Run(async () => {\n    while (hkb.IsActive) {\n        var visc = hkb.MeasureViscosity();\n        if (visc < VISC_MIN_CST ||\n                visc > VISC_MAX_CST) {\n            log.Warn(\n                $"hkb fluid visc {visc:F2}cSt out of band");\n            hkb.FlagFluidService();\n        }\n        viscHistory.Add(visc);\n        if (viscHistory.Count > VISC_HIST_SIZE) {\n            viscHistory.RemoveAt(0);\n        }\n        await Task.Delay(\n            VISC_INTERVAL_MS, cts.Token);\n    }\n});',
        'foreach (var damper in hkb.Dampers) {\n    var wear = wearModel.Estimate(\n        damper.Cycles,\n        damper.AvgEnergyJ,\n        damper.FluidAgeHours);\n    if (wear > WEAR_REPLACE_THRESH) {\n        log.Error(\n            $"damper {damper.Id} wear {wear:F2}");\n        hkb.FlagDamperReplace(damper.Id);\n    } else if (wear > WEAR_WARN_THRESH) {\n        log.Warn(\n            $"damper {damper.Id} wear {wear:F2}");\n    }\n    metrics.DamperWear[damper.Id] = wear;\n}',
        'while (hkb.HasStrokeQueue) {\n    var stroke = hkb.DequeueStroke();\n    var env = envelope.Sample(stroke.Time);\n    if (Math.Abs(stroke.Force - env.Expected) >\n            env.ToleranceN) {\n        log.Warn(\n            $"hkb stroke envelope deviation {stroke.Force - env.Expected:F1}N");\n        envelope.Adapt(stroke.Force);\n    }\n    if (++strokeCount % LOG_EVERY == 0) {\n        log.Debug(\n            $"hkb strokes={strokeCount}");\n    }\n}',
        'for (int s = 0; s < hkb.Seals.Count; s++) {\n    var leak = hkb.MeasureSealLeak(s);\n    if (leak > SEAL_LEAK_LIMIT) {\n        log.Error(\n            $"hkb seal{s} leak {leak:F2}mL/min");\n        hkb.IsolateChamber(s);\n        sealLeaks.Add(s);\n        if (sealLeaks.Count > MAX_SEAL_LEAKS) {\n            hkb.RequestShutdown();\n            throw new HkbSealException(\n                sealLeaks.Count);\n        }\n    }\n}',
        'while (hkb.Regulator.NeedsAdjust) {\n    var current = hkb.Regulator.ReadPsi();\n    var target  = hkb.Regulator.TargetPsi();\n    var err = target - current;\n    if (Math.Abs(err) < REGULATOR_DEADBAND)\n        break;\n    hkb.Regulator.Adjust(err * REGULATOR_KP);\n    await Task.Delay(REGULATOR_STEP_MS);\n    if (++iters > REGULATOR_MAX_ITERS) {\n        log.Warn(\n            $"regulator did not converge: err={err:F1}psi");\n        hkb.Regulator.Fault();\n        break;\n    }\n}',
        'Task.Run(async () => {\n    while (hkb.IsActive) {\n        var snap = hkb.Telemetry.Snapshot();\n        await telemetry.EmitAsync(\n            "hkb.psi",     snap.Psi);\n        await telemetry.EmitAsync(\n            "hkb.fluid",   snap.FluidPct);\n        await telemetry.EmitAsync(\n            "hkb.cycles",  snap.Cycles);\n        await telemetry.EmitAsync(\n            "hkb.wear",    snap.WearScore);\n        await Task.Delay(\n            TELEMETRY_INTERVAL_MS, cts.Token);\n    }\n});',
        'for (int p = 0; p < hkb.AlignPoints.Count; p++) {\n    var offset = hkb.MeasureAlign(p);\n    if (Math.Abs(offset) > ALIGN_TOLERANCE_MM) {\n        log.Warn(\n            $"hkb align p{p} offset {offset:F2}mm");\n        await hkb.AdjustAlignAsync(p,\n            -offset * ALIGN_GAIN);\n        adjusted++;\n        if (adjusted > MAX_ALIGN_ADJUSTMENTS) {\n            log.Error("hkb alignment — service required");\n            audit.Record(AuditEvent.HkbAlignFault);\n            break;\n        }\n    }\n}',
        'while (rebound.IsMeasuring) {\n    var t = rebound.ElapsedMs;\n    var pos = rebound.Position;\n    if (pos > REBOUND_TARGET_MM) {\n        var dt = t - lastT;\n        if (dt < REBOUND_MIN_MS) {\n            log.Warn(\n                $"rebound too fast {dt}ms");\n            hkb.IncreaseDamping(\n                DAMPING_STEP);\n        }\n        break;\n    }\n    lastT = t;\n    await Task.Yield();\n}',
        'foreach (var batch in trainSet.Shuffle()\n    .Batch(BATCH_SIZE)) {\n    var pred = bim.Forward(batch.Inputs);\n    var loss = crossEntropy.Compute(\n        pred, batch.Labels);\n    bim.Backward(loss);\n    if (clipNorm > 0) {\n        bim.ClipGradients(clipNorm);\n    }\n    optimizer.Step(lr);\n    optimizer.ZeroGrad();\n    runningLoss = runningLoss * 0.9f +\n        loss * 0.1f;\n}',
        'for (int f = 0; f < features.Length; f++) {\n    var m = stats.Mean(f);\n    var s = stats.Std(f);\n    if (s < EPSILON) {\n        features[f] = 0f;\n        continue;\n    }\n    features[f] = (features[f] - m) / s;\n    if (Math.Abs(features[f]) > CLIP_SIGMA) {\n        features[f] =\n            Math.Sign(features[f]) * CLIP_SIGMA;\n        clipped++;\n    }\n}',
        'while (ensemble.HasModel) {\n    var m = ensemble.NextModel();\n    var pred = m.Predict(input);\n    voteMap[pred.Class] =\n        voteMap.GetValueOrDefault(pred.Class)\n        + pred.Confidence;\n    if (voteMap[pred.Class] >=\n            ENSEMBLE_QUORUM) {\n        return new EnsembleResult(\n            pred.Class,\n            voteMap[pred.Class]);\n    }\n}\nreturn EnsembleResult.NoConsensus(voteMap);',
        'foreach (var hp in hpSpace.Sample(\n    TRIALS, rng)) {\n    bim.Configure(hp);\n    var acc = await bim.TrainAndEvalAsync(\n        trainFold, valFold, cts.Token);\n    log.Info(\n        $"trial: lr={hp.Lr:F4} drop={hp.Dropout:F2} acc={acc:P1}");\n    if (acc > bestAcc) {\n        bestAcc = acc;\n        bestHp  = hp;\n    }\n    if (cts.IsCancellationRequested) break;\n}',
        'for (int fold = 0; fold < K_FOLDS; fold++) {\n    var (train, test) =\n        dataset.SplitFold(fold, K_FOLDS);\n    bim.Reset();\n    await bim.TrainAsync(train, cts.Token);\n    var acc = bim.Evaluate(test);\n    foldAccuracies.Add(acc);\n    log.Debug(\n        $"fold {fold}: acc={acc:P1}");\n}\nvar mean = foldAccuracies.Average();\nvar std  = foldAccuracies.StdDev();\nlog.Info(\n    $"cv: mean={mean:P1} std={std:P1}");',
        'while (model.GradNorm > CONVERGE_TOL\n    && step < MAX_STEPS) {\n    var (x, y) = batches.NextOrLoop();\n    var pred = model.Forward(x);\n    var loss = loss_fn(pred, y);\n    model.Backward(loss);\n    var norm = model.ComputeGradNorm();\n    if (norm > CLIP_NORM) {\n        model.ScaleGradients(CLIP_NORM / norm);\n    }\n    optimizer.Step(lr);\n    step++;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        if (step % CHECKPOINT_EVERY == 0) {\n            var path = checkpointDir\n                + $"/ckpt-{step}.bin";\n            await model.SaveAsync(\n                path, cts.Token);\n            log.Info(\n                $"checkpoint @ step {step}");\n            checkpoints.Add(path);\n            if (checkpoints.Count >\n                    MAX_CHECKPOINTS) {\n                File.Delete(checkpoints[0]);\n                checkpoints.RemoveAt(0);\n            }\n        }\n        await Task.Delay(\n            CHECKPOINT_POLL_MS, cts.Token);\n    }\n});',
        'foreach (var sample in batch) {\n    var z = adversary.DetectScore(sample);\n    if (z > ADV_THRESHOLD) {\n        log.Warn(\n            $"adversarial: z={z:F2} → reject");\n        rejected.Add(sample);\n        continue;\n    }\n    var pred = bim.Predict(sample);\n    if (pred.Confidence < MIN_CONFIDENCE) {\n        deferred.Add(sample);\n        continue;\n    }\n    results.Add(pred);\n}',
        'while (inferDeadline > DateTime.UtcNow) {\n    var pred = bim.PartialInfer(input);\n    if (pred.Confidence >= EARLY_EXIT_CONF) {\n        return pred;\n    }\n    if (bim.RemainingLayers == 0) break;\n    bim.AdvanceLayer();\n}\nlog.Warn(\n    $"infer budget exceeded: layers={bim.LayersDone}");\nreturn bim.FinalInfer(input);',
        'for (int i = 0; i < attestRounds; i++) {\n    var nonce = rng.NextBytes(NONCE_LEN);\n    var quote = await enclave\n        .AttestAsync(nonce, cts.Token);\n    if (!quote.VerifyAgainstPcr(\n            expectedPcr, mrEnclave)) {\n        log.Error(\n            $"attest round {i} failed");\n        attestFails++;\n        if (attestFails > MAX_ATTEST_FAILS) {\n            await lockdown.EngageAsync();\n            throw new AttestationException(\n                attestFails);\n        }\n    } else {\n        attestFails = 0;\n    }\n}',
        'using (var seal = enclave.OpenSeal(\n    SealPolicy.MrEnclave)) {\n    try {\n        var wrapped = seal.Wrap(secret);\n        await vault.StoreAsync(\n            wrapped.KeyId, wrapped.Blob);\n        audit.Record(\n            AuditEvent.SecretSealed,\n            wrapped.KeyId);\n    } catch (SealException ex) {\n        log.Error(\n            $"seal failed: {ex.Message}");\n        audit.Record(\n            AuditEvent.SealFailed,\n            ex.Reason);\n        throw;\n    }\n}',
        'while (token.IsExpiringSoon(REFRESH_BEFORE)) {\n    try {\n        token = await corp\n            .RefreshTokenAsync(\n                token, cts.Token);\n        if (!token.IsValid) {\n            log.Warn(\n                $"refresh denied: {token.Reason}");\n            await Task.Delay(\n                REFRESH_BACKOFF_MS << retries,\n                cts.Token);\n            retries++;\n            continue;\n        }\n        session.UpdateToken(token);\n        retries = 0;\n        break;\n    } catch (TokenRevokedException) {\n        await session.LogoutAsync();\n        throw;\n    }\n}',
        'for (int factor = 0; factor < mfaFactors.Length; factor++) {\n    var challenge = mfaFactors[factor]\n        .GenerateChallenge();\n    var response = await operator2\n        .RespondAsync(challenge,\n                      MFA_TIMEOUT_MS,\n                      cts.Token);\n    if (response.IsValid) {\n        passed.Add(factor);\n        if (passed.Count >= MFA_REQUIRED) {\n            session.MarkMfaSatisfied();\n            return;\n        }\n        continue;\n    }\n    log.Warn(\n        $"mfa factor {factor} failed");\n    if (++failures >= MFA_LOCKOUT) {\n        await account.LockAsync(\n            operator2.Id);\n        throw new MfaLockoutException();\n    }\n}',
        'while (!sso.IsResolved) {\n    var redirect = await sso\n        .GetRedirectAsync(cts.Token);\n    if (redirect == null) break;\n    var resp = await http\n        .GetAsync(redirect.Url,\n                  cts.Token);\n    if (resp.StatusCode == 302) {\n        sso.FollowRedirect(\n            resp.Headers.Location);\n        if (++redirectCount > MAX_REDIRECTS) {\n            throw new RedirectLoopException();\n        }\n    } else if (resp.StatusCode == 200) {\n        sso.ResolveFromResponse(resp);\n    } else {\n        throw new SsoException(\n            resp.StatusCode);\n    }\n}',
        'foreach (var role in operator2.Roles) {\n    var perms = await rbac\n        .GetPermissionsAsync(role,\n                             cts.Token);\n    foreach (var p in perms) {\n        if (effective.ContainsKey(p.Resource)) {\n            effective[p.Resource] |= p.Actions;\n        } else {\n            effective[p.Resource] = p.Actions;\n        }\n    }\n    if (role.IsDeny) {\n        foreach (var p in perms) {\n            denied[p.Resource] |= p.Actions;\n        }\n    }\n}',
        'Task.Run(async () => {\n    while (session.IsActive) {\n        var idle = DateTime.UtcNow -\n            session.LastActivity;\n        if (idle > IDLE_LOGOUT) {\n            log.Info(\n                $"session {session.Id} idle {idle.TotalMinutes:F0}m → logout");\n            await session.LogoutAsync(\n                LogoutReason.Idle);\n            return;\n        }\n        if (idle > IDLE_WARN) {\n            await session.NotifyIdleAsync();\n        }\n        await Task.Delay(\n            IDLE_POLL_MS, cts.Token);\n    }\n});',
        'for (int i = 0; i < credentials.Count; i++) {\n    var c = credentials[i];\n    if (c.Age > CREDENTIAL_MAX_AGE) {\n        var newCred = await issuer\n            .RotateAsync(c, cts.Token);\n        await services.PropagateAsync(\n            c.Id, newCred.Id, cts.Token);\n        credentials[i] = newCred;\n        audit.Record(\n            AuditEvent.CredentialRotated,\n            c.Id, newCred.Id);\n        rotated++;\n    }\n}',
        'while (auditChain.HasNext) {\n    var ev = auditChain.Next();\n    var hash = sha256.Hash(\n        ev.Serialize());\n    if (ev.PrevHash != lastHash) {\n        log.Error(\n            $"audit chain break @ {ev.Id}");\n        breaks.Add((lastEv?.Id, ev.Id));\n        if (breaks.Count > MAX_CHAIN_BREAKS) {\n            await sec.RaiseIncidentAsync(\n                IncidentType.AuditChainBroken);\n            break;\n        }\n    }\n    lastHash = hash;\n    lastEv = ev;\n    verified++;\n}',
        'foreach (var (resource, delta) in permDeltas) {\n    if (delta.Add != Action.None) {\n        await rbac.GrantAsync(\n            principal, resource, delta.Add);\n        audit.Record(\n            AuditEvent.PermGranted,\n            principal, resource, delta.Add);\n    }\n    if (delta.Remove != Action.None) {\n        await rbac.RevokeAsync(\n            principal, resource, delta.Remove);\n        audit.Record(\n            AuditEvent.PermRevoked,\n            principal, resource, delta.Remove);\n    }\n}',
        'while (!route.IsResolved) {\n    var hop = await glmz\n        .NextHopAsync(route.Destination,\n                      cts.Token);\n    if (hop == null) {\n        if (route.Hops.Count == 0) {\n            throw new NoRouteException(\n                route.Destination);\n        }\n        var prev = route.Hops.Last();\n        prev.Blacklist();\n        route.Hops.RemoveAt(\n            route.Hops.Count - 1);\n        continue;\n    }\n    route.AddHop(hop);\n    if (route.Hops.Count > MAX_HOP_COUNT) {\n        throw new RouteTooLongException(\n            route.Hops.Count);\n    }\n}',
        'Task.Run(async () => {\n    while (darkNode.IsConnected) {\n        if (darkNode.FramesSinceRekey >\n                REKEY_FRAMES) {\n            await darkNode.RekeyAsync(\n                cts.Token);\n            log.Info(\n                $"dark-node rekey ok");\n            darkNode.FramesSinceRekey = 0;\n        }\n        var frame = await darkNode\n            .RecvAsync(cts.Token);\n        await processor.HandleAsync(\n            frame, cts.Token);\n        darkNode.FramesSinceRekey++;\n    }\n});',
        'for (int g = 0; g < gateways.Count; g++) {\n    var probe = await gateways[g]\n        .ProbeAsync(PROBE_TIMEOUT_MS,\n                    cts.Token);\n    if (!probe.IsReachable) {\n        gateways[g].MissCount++;\n        if (gateways[g].MissCount >\n                GATEWAY_DEAD_THRESH) {\n            gateways[g].MarkDead();\n            log.Warn(\n                $"gateway {gateways[g].Id} dead");\n        }\n        continue;\n    }\n    gateways[g].MissCount = 0;\n    gateways[g].Rtt = probe.Rtt;\n}',
        'while (window.HasUnacked) {\n    var pkt = window.NextUnacked();\n    if (pkt.SentAt + RTT_BUDGET <\n            DateTime.UtcNow) {\n        await net.ResendAsync(\n            pkt, cts.Token);\n        pkt.RetryCount++;\n        if (pkt.RetryCount > MAX_RETRIES) {\n            log.Error(\n                $"packet {pkt.Seq} max retries — drop");\n            window.Drop(pkt.Seq);\n            droppedPackets++;\n        }\n    }\n    if (window.AckedAhead(pkt.Seq, ACK_AGG)) {\n        window.SlideTo(pkt.Seq + ACK_AGG);\n    }\n}',
        'foreach (var path in multipath.Paths) {\n    if (!path.IsHealthy) continue;\n    try {\n        var result = await path\n            .SendAsync(payload, cts.Token);\n        if (result.IsSuccess) {\n            metrics.MultipathSent++;\n            return result;\n        }\n    } catch (PathFailedException ex) {\n        log.Warn(\n            $"path {path.Id} failed: {ex.Reason}");\n        path.MarkUnhealthy();\n        if (--healthyPaths == 0) {\n            throw new AllPathsFailedException();\n        }\n    }\n}',
        'while (stun.State != StunState.Bound) {\n    var resp = await stun.SendBindingAsync(\n        STUN_SERVER, cts.Token);\n    if (resp.ChangedAddress != null) {\n        stun.PublicEndpoint =\n            resp.MappedAddress;\n        stun.State = StunState.Bound;\n        break;\n    }\n    if (++attempts > STUN_MAX_ATTEMPTS) {\n        throw new StunBindException(attempts);\n    }\n    await Task.Delay(\n        STUN_RETRY_MS * attempts, cts.Token);\n}',
        'for (int i = 0; i < dnsCache.Count; i++) {\n    var entry = dnsCache[i];\n    if (entry.AnswerCount > MAX_ANSWERS) {\n        log.Warn(\n            $"dns cache {entry.Name}: {entry.AnswerCount} answers — suspect");\n        dnsCache.Quarantine(entry);\n        continue;\n    }\n    if (entry.Ttl == 0 ||\n            entry.ResolvedAt + entry.Ttl <\n                DateTime.UtcNow) {\n        dnsCache.Invalidate(entry.Name);\n    }\n}',
        'while (tls.IsHandshaking) {\n    var msg = await tls\n        .NextHandshakeAsync(cts.Token);\n    if (msg.IsResumption) {\n        var ticket = tls.GetTicket();\n        if (ticket?.IsValid == true) {\n            await tls.ResumeAsync(\n                ticket, cts.Token);\n            log.Debug(\n                "tls session resumed");\n            return;\n        }\n    }\n    await tls.ProcessAsync(\n        msg, cts.Token);\n}',
        'Task.Run(async () => {\n    for (int i = 0; i < pool.MinSize; i++) {\n        try {\n            var conn = await connFactory\n                .CreateAsync(cts.Token);\n            pool.Return(conn);\n        } catch (Exception ex) {\n            log.Warn(\n                $"pool warmup #{i} fail: {ex.Message}");\n            if (i == 0) throw;\n        }\n    }\n    log.Info(\n        $"pool warmed: {pool.Available}/{pool.MinSize}");\n});',
        'while (shaper.HasCredits) {\n    var pkt = ingress.Peek();\n    if (pkt == null) {\n        await Task.Yield();\n        continue;\n    }\n    if (!shaper.TryConsume(pkt.Size)) {\n        await Task.Delay(\n            shaper.NextRefillMs, cts.Token);\n        continue;\n    }\n    ingress.Dequeue();\n    await egress.SendAsync(\n        pkt, cts.Token);\n    metrics.Shaped++;\n}',
        'foreach (var sst in level.GetSstables()) {\n    var bloom = await sst\n        .RebuildBloomAsync(\n            BLOOM_BITS_PER_KEY,\n            cts.Token);\n    sst.AttachBloom(bloom);\n    log.Debug(\n        $"sst {sst.Id} bloom rebuilt: {bloom.Bits} bits");\n    rebuilt++;\n    if (rebuilt >= MAX_REBUILD_BATCH)\n        break;\n}',
        'while (wal.HasRecoverableFrames) {\n    var frame = wal.NextFrame();\n    if (frame.Lsn <= lastApplied) continue;\n    try {\n        await db.ApplyFrameAsync(\n            frame, cts.Token);\n        lastApplied = frame.Lsn;\n        if (lastApplied % WAL_CKPT == 0) {\n            await db.CheckpointAsync();\n            await wal.TruncateBeforeAsync(\n                lastApplied);\n            log.Info(\n                $"wal ckpt @lsn={lastApplied:X}");\n        }\n    } catch (FrameApplyException ex) {\n        log.Error(\n            $"wal frame {frame.Lsn:X} apply fail");\n        throw;\n    }\n}',
        'for (int i = 0; i < mvcc.Versions.Count; i++) {\n    var v = mvcc.Versions[i];\n    if (v.CommitLsn < snapshotHorizon) {\n        mvcc.Remove(v);\n        purged++;\n        i--;\n        continue;\n    }\n    if (v.IsTombstone &&\n            v.CreatedAt + TOMBSTONE_TTL <\n                DateTime.UtcNow) {\n        mvcc.Remove(v);\n        purged++;\n        i--;\n    }\n}',
        'foreach (var node in btree\n    .ScanLeaves()\n    .Where(n => n.TombstoneCount >\n        n.LiveCount)) {\n    var rebuilt = await btree\n        .RewriteNodeAsync(\n            node, cts.Token);\n    if (rebuilt.LiveCount == 0) {\n        await btree.MergeUpAsync(\n            rebuilt, cts.Token);\n    }\n    metrics.NodesCompacted++;\n}',
        'while (cache.PinnedPages > MAX_PINNED) {\n    var oldest = cache.OldestPinned();\n    if (oldest.RefCount == 0) {\n        cache.Unpin(oldest);\n        cache.Evict(oldest.Id);\n        unpinned++;\n        continue;\n    }\n    log.Warn(\n        $"page {oldest.Id} pinned {oldest.RefCount}× — wait");\n    await Task.Delay(\n        PIN_WAIT_MS, cts.Token);\n    if (++spins > MAX_SPINS) {\n        throw new PinExhaustionException();\n    }\n}',
        'for (int level = 0;\n     level < lsm.MaxLevel; level++) {\n    var files = lsm.GetLevel(level);\n    if (files.Count <\n            lsm.LevelTrigger(level)) continue;\n    var merged = await Sstable\n        .CompactAsync(files,\n            lsm.NewOutput(level + 1),\n            cts.Token);\n    foreach (var f in files) {\n        f.RetireAsync();\n    }\n    lsm.Promote(merged, level + 1);\n    log.Info(\n        $"lsm L{level}→L{level+1}: {files.Count}→1");\n}',
        'while (delta.HasOps) {\n    var op = delta.NextOp();\n    switch (op.Type) {\n        case DeltaOp.Insert:\n            target.Insert(op.Key, op.Value);\n            break;\n        case DeltaOp.Update:\n            target.Update(op.Key, op.Value);\n            break;\n        case DeltaOp.Delete:\n            target.Delete(op.Key);\n            break;\n    }\n    if (++applied % SYNC_EVERY == 0) {\n        await target.SyncAsync();\n    }\n}',
        'Task.Run(async () => {\n    using var idx = db.OpenIndexBuilder(\n        indexName);\n    await foreach (var row in db\n        .ScanTableAsync(table,\n                        cts.Token)) {\n        var keys = projection.Extract(row);\n        foreach (var k in keys) {\n            idx.Add(k, row.RowId);\n        }\n        if (++processed % FLUSH_EVERY == 0) {\n            await idx.FlushAsync(cts.Token);\n            log.Debug(\n                $"idx {indexName} progress: {processed} rows");\n        }\n    }\n    await idx.FinalizeAsync(cts.Token);\n});',
        'while (writeMeter.HasData) {\n    var write = writeMeter.Sample();\n    var amp   = write.BytesToDisk /\n        (float)write.BytesLogical;\n    if (amp > AMPLIFY_WARN) {\n        log.Warn(\n            $"write amp {amp:F2}× ({write.BytesToDisk}/{write.BytesLogical})");\n        if (amp > AMPLIFY_CRIT) {\n            await alerting.FireAsync(\n                Alert.WriteAmplification, amp);\n        }\n    }\n    metrics.WriteAmp = amp;\n}',
        'for (int s = 0; s < SECTORS; s++) {\n    var err = disk.ReadErrorRate(s);\n    if (err > SECTOR_ERR_LIMIT) {\n        log.Error(\n            $"disk sector {s} err rate {err:F4}");\n        await disk.RemapAsync(s, cts.Token);\n        remapped++;\n        if (remapped > MAX_REMAPS) {\n            log.Fatal("disk failing — replace");\n            await alerting.PageOncallAsync(\n                Alert.DiskFailing);\n            break;\n        }\n    }\n}',
        'foreach (var msg in batch) {\n    var sig = ed25519.Sign(\n        signingKey, msg.Payload);\n    msg.Signature = sig;\n    msg.SignedAt = DateTime.UtcNow;\n    if (sigBuf.Count >= BATCH_SIG_FLUSH) {\n        await net.SendBatchAsync(\n            sigBuf, cts.Token);\n        sigBuf.Clear();\n    }\n    sigBuf.Add(msg);\n    metrics.Signed++;\n}\nif (sigBuf.Count > 0) {\n    await net.SendBatchAsync(sigBuf, cts.Token);\n}',
        'while (handshake.NeedsExchange) {\n    var pub = x25519.GeneratePublic(\n        ephemeralPriv);\n    await net.SendAsync(pub, cts.Token);\n    var peerPub = await net\n        .RecvAsync(cts.Token);\n    var shared = x25519.ComputeShared(\n        ephemeralPriv, peerPub);\n    sessionKey = hkdf.Expand(\n        shared, salt, KEY_LEN);\n    handshake.MarkComplete();\n    log.Info("ecdh established");\n}',
        'for (int i = 0; i < commits.Count; i++) {\n    var c = commits[i];\n    var expected = (i == 0)\n        ? GENESIS_HASH\n        : commits[i - 1].Hash;\n    if (c.PrevHash != expected) {\n        log.Error(\n            $"chain break @ {i}: {c.PrevHash:X} != {expected:X}");\n        throw new ChainBrokenException(i);\n    }\n    var hash = sha256.Hash(c.Serialize());\n    if (hash != c.Hash) {\n        log.Error(\n            $"commit {i} hash mismatch");\n        throw new TamperException(i);\n    }\n}',
        'foreach (var shard in vault\n    .EnumerateShards()) {\n    var newKey = await kms\n        .RotateAsync(shard.KeyId, cts.Token);\n    var plain = shard.Decrypt(shard.OldKey);\n    var wrapped = newKey.Wrap(plain);\n    await vault.UpdateAsync(\n        shard.Id, wrapped, newKey.Id);\n    audit.Record(\n        AuditEvent.ShardReencrypted,\n        shard.Id);\n    rotated++;\n}',
        'while (verify.HasItem) {\n    var (mac, expected) = verify.Next();\n    var actual = hmac.Compute(\n        verifyKey, mac.Payload);\n    int diff = 0;\n    for (int i = 0; i < actual.Length; i++) {\n        diff |= actual[i] ^ expected[i];\n    }\n    if (diff != 0) {\n        log.Error(\n            $"hmac mismatch msg {mac.Id}");\n        invalid++;\n        if (invalid > MAX_INVALID) {\n            await sec.RaiseIncidentAsync(\n                IncidentType.HmacFlood);\n            break;\n        }\n    }\n}',
        'foreach (var nonce in incoming\n    .GroupBy(p => p.Nonce)\n    .Where(g => g.Count() > 1)) {\n    log.Error(\n        $"nonce reuse: {nonce.Key:X} ({nonce.Count()} pkts)");\n    foreach (var p in nonce) {\n        p.Quarantine();\n    }\n    await sec.RaiseIncidentAsync(\n        IncidentType.NonceReuse);\n    reuse++;\n    if (reuse > MAX_REUSE_TOLERATED) {\n        await session.RekeyAsync(cts.Token);\n        break;\n    }\n}',
        'for (int i = 0; i < KDF_ITERS; i++) {\n    derived = pbkdf2.Iterate(\n        derived, salt, HASH);\n    if (i % PROGRESS_EVERY == 0) {\n        progress?.Report(\n            i / (float)KDF_ITERS);\n    }\n}\nif (derived.Length != KEY_LEN) {\n    throw new KdfLengthException(\n        derived.Length, KEY_LEN);\n}\nlog.Debug(\n    $"pbkdf2 {KDF_ITERS} iters done");',
        'while (chain.HasNext) {\n    var cert = chain.Next();\n    if (!pinning.IsPinned(cert.Thumbprint,\n                          cert.Subject)) {\n        log.Error(\n            $"cert {cert.Subject} not pinned");\n        throw new PinningException(\n            cert.Subject);\n    }\n    if (revocation.IsRevoked(\n            cert.Thumbprint)) {\n        throw new RevokedException(\n            cert.Subject);\n    }\n    verified++;\n}',
        'foreach (var sig in pendingSigs) {\n    var tsa = await tsaClient\n        .RequestAsync(sig.Payload, cts.Token);\n    if (!tsa.Verify(tsaCert)) {\n        log.Warn(\n            $"tsa response for {sig.Id} invalid");\n        invalid++;\n        continue;\n    }\n    sig.TimestampAnchor = tsa.Token;\n    await signedStore.PersistAsync(\n        sig, cts.Token);\n    anchored++;\n}',
        'while (kdf.NeedsRotation) {\n    var newSalt = rng.NextBytes(SALT_LEN);\n    var newKey  = kdf.Derive(\n        masterSeed, newSalt, KDF_ITERS);\n    foreach (var consumer in keyConsumers) {\n        await consumer.UpdateKeyAsync(\n            newKey, cts.Token);\n    }\n    kdf.MarkRotated(newSalt);\n    audit.Record(\n        AuditEvent.KdfRotated);\n    break;\n}',
        'while (queue.HasItem) {\n    if (!workStealing.LocalQueue.TryPop(\n            out var task)) {\n        var stolen = workStealing\n            .StealFromRandomVictim();\n        if (stolen == null) {\n            await Task.Yield();\n            continue;\n        }\n        task = stolen;\n        metrics.Stolen++;\n    } else {\n        metrics.Local++;\n    }\n    await task.RunAsync(cts.Token);\n}',
        'foreach (var holder in lock2.Holders\n    .OrderByDescending(\n        h => h.EffectivePriority)) {\n    if (holder.NeedsPriorityBoost(\n            waitingPriority)) {\n        holder.BoostTo(waitingPriority);\n        log.Debug(\n            $"pi boost {holder.Tid} → {waitingPriority}");\n        boosted++;\n    }\n    if (holder.Priority == MAX_PRIORITY) {\n        break;\n    }\n}',
        'for (int t = 0; t < threads.Count; t++) {\n    var cpu = affinity.PreferredCpu(\n        threads[t].WorkClass);\n    if (threads[t].CurrentCpu != cpu) {\n        await scheduler\n            .BindAffinityAsync(\n                threads[t].Tid, cpu);\n        log.Debug(\n            $"tid {threads[t].Tid} bound cpu{cpu}");\n        rebound++;\n    }\n}',
        'while (coro.HasBudget) {\n    coro.Step();\n    if (coro.BudgetRemaining < BUDGET_LOW) {\n        await Task.Yield();\n        coro.RefreshBudget();\n        yielded++;\n        if (yielded > MAX_YIELDS_PER_TICK) {\n            log.Warn(\n                $"coro {coro.Id} yield storm");\n            await Task.Delay(\n                STALL_RECOVER_MS);\n            break;\n        }\n    }\n}',
        'Task.Run(async () => {\n    using var timer = new TimerWheel(\n        BUCKET_COUNT, TICK_MS);\n    while (!cts.IsCancellationRequested) {\n        var fired = timer.AdvanceTick();\n        foreach (var t in fired) {\n            try {\n                await t.Callback(cts.Token);\n                metrics.TimersFired++;\n            } catch (Exception ex) {\n                log.Warn(\n                    $"timer {t.Id} cb: {ex.Message}");\n            }\n        }\n        await Task.Delay(\n            TICK_MS, cts.Token);\n    }\n});',
        'foreach (var child in scope.Children) {\n    if (child.IsLinked) {\n        child.PropagateCancel(reason);\n    }\n    if (child.WaitOnDispose) {\n        await child.WaitAsync(\n            CANCEL_WAIT_MS, cts.Token);\n    }\n    child.Detach();\n    detached++;\n}\nlog.Debug(\n    $"cancel propagated: {detached} children");',
        'while (attempts < MAX_RETRY) {\n    try {\n        return await op.RunAsync(cts.Token);\n    } catch (TransientException ex) {\n        attempts++;\n        var jitter = rng.Next(\n            -JITTER_MS, JITTER_MS);\n        var delay = Math.Min(\n            BASE_MS * (1 << attempts) + jitter,\n            MAX_DELAY_MS);\n        log.Warn(\n            $"op fail #{attempts}: {ex.Message} retry {delay}ms");\n        await Task.Delay(\n            delay, cts.Token);\n    }\n}',
        'for (int i = 0; i < queues.Count; i++) {\n    var weight = queues[i].Weight;\n    var deserved = totalCapacity *\n        (weight / totalWeight);\n    var actual = queues[i].Consumed;\n    if (actual > deserved * OVERSHOOT_TOL) {\n        queues[i].Throttle(\n            actual - deserved);\n        log.Debug(\n            $"q{i} throttled by {actual - deserved}");\n    }\n}',
        'while (pendingOps.Count > 0) {\n    if (pendingOps.Count > OP_CAP) {\n        log.Warn(\n            $"op cap {OP_CAP} reached — backpressure");\n        backpressure.Engage();\n        await pendingOps.WaitDrainAsync(\n            cts.Token);\n        backpressure.Release();\n    }\n    var op = pendingOps.Dequeue();\n    _ = Task.Run(\n        async () => await op\n            .ExecuteAsync(cts.Token));\n}',
        'Task.Run(async () => {\n    while (!shutdown.IsCancellationRequested) {\n        await Task.Delay(\n            POLL_MS, cts.Token);\n    }\n    foreach (var svc in services\n        .OrderByDescending(\n            s => s.ShutdownPriority)) {\n        log.Info(\n            $"stopping {svc.Name}");\n        await svc.StopAsync(\n            STOP_TIMEOUT_MS);\n    }\n    log.Info("shutdown complete");\n});',
        'foreach (var sample in latencyStream) {\n    p99.Add(sample);\n    if (p99.Count > P99_WINDOW) {\n        p99.RemoveAt(0);\n    }\n    if (p99.Count >= P99_MIN) {\n        var p99v = p99\n            .Percentile(99);\n        await telemetry.EmitAsync(\n            "latency.p99", p99v);\n        if (p99v > SLO_P99_MS) {\n            await alerting.FireAsync(\n                Alert.SloP99Breach, p99v);\n        }\n    }\n}',
        'for (int x = 0; x < grid.W; x++) {\n    for (int y = 0; y < grid.H; y++) {\n        var v = grid[x, y];\n        if (v == 0) continue;\n        var bucket = (int)(\n            v / heatmap.Resolution);\n        heatmap.Buckets[bucket]++;\n        heatmap.Coords.Add((x, y, bucket));\n    }\n}\nheatmap.Normalize();\nlog.Debug(\n    $"heatmap: buckets={heatmap.Buckets.Length}");',
        'while (errorBudget.HasRemaining) {\n    var rate = errorMeter.RatePerMin;\n    var consumption = rate / SLO_TARGET_RATE;\n    errorBudget.Consume(consumption);\n    if (errorBudget.Remaining < BUDGET_WARN) {\n        log.Warn(\n            $"slo budget {errorBudget.Remaining:P1} left");\n    }\n    if (errorBudget.Remaining <= 0) {\n        await releaseGate\n            .FreezeAsync(\n                FreezeReason.SloBudgetExhausted);\n        break;\n    }\n    await Task.Delay(\n        BUDGET_INTERVAL_MS, cts.Token);\n}',
        'foreach (var span in trace.Spans) {\n    if (sampling.ShouldSample(span)) {\n        await spanSink.WriteAsync(\n            span, cts.Token);\n        sampled++;\n    }\n    if (span.Duration > TRACE_SLOW) {\n        await spanSink.ForceWriteAsync(\n            span, cts.Token);\n        forced++;\n    }\n    if (span.HasError) {\n        await alertSink.WriteAsync(\n            span, cts.Token);\n    }\n}',
        'while (anomaly.HasInput) {\n    var (key, value) = anomaly.Next();\n    var z = anomaly.ZScore(key, value);\n    if (Math.Abs(z) > ANOMALY_Z) {\n        log.Warn(\n            $"anomaly {key}={value} z={z:F2}");\n        await siem.IngestAsync(\n            new AnomalyEvent(\n                key, value, z,\n                DateTime.UtcNow));\n        recentAnomalies++;\n        if (recentAnomalies >\n                ANOMALY_STORM) {\n            await alerting.FireAsync(\n                Alert.AnomalyStorm);\n            recentAnomalies = 0;\n        }\n    }\n}',
        'for (int i = 0; i < probes.Count; i++) {\n    try {\n        var r = await probes[i]\n            .CheckAsync(cts.Token);\n        rollup[probes[i].Name] = r.Status;\n        if (r.Status != HealthStatus.Healthy) {\n            unhealthy.Add(probes[i].Name);\n        }\n    } catch (Exception ex) {\n        rollup[probes[i].Name] =\n            HealthStatus.Failed;\n        unhealthy.Add(probes[i].Name);\n        log.Warn(\n            $"probe {probes[i].Name}: {ex.Message}");\n    }\n}',
        'foreach (var alert in alerts) {\n    var key = alert.DedupeKey();\n    if (recentAlerts.TryGetValue(\n            key, out var prev)) {\n        if (DateTime.UtcNow - prev.LastFired <\n                DEDUPE_WINDOW) {\n            prev.RepeatCount++;\n            deduped++;\n            continue;\n        }\n    }\n    recentAlerts[key] = alert;\n    await alertSink.FireAsync(\n        alert, cts.Token);\n    fired++;\n}',
        'while (exporter.HasMetric) {\n    var m = exporter.Next();\n    try {\n        await exporter.WriteAsync(\n            new MetricPayload {\n                Name      = m.Name,\n                Value     = m.Value,\n                Labels    = m.Labels,\n                Timestamp = m.Timestamp\n            }, cts.Token);\n        exported++;\n    } catch (Exception ex) {\n        log.Warn(\n            $"export {m.Name} fail: {ex.Message}");\n        exporter.RequeueAsync(m);\n    }\n}',
        'for (int i = 0; i < logBatch.Count; i++) {\n    var line = logBatch[i];\n    foreach (var pattern in redactPatterns) {\n        line = pattern.Replace(\n            line, REDACT_TOKEN);\n    }\n    if (line.Length > MAX_LOG_LEN) {\n        line = line.Substring(\n            0, MAX_LOG_LEN) + "…";\n        truncated++;\n    }\n    logBatch[i] = line;\n}\nlog.Debug(\n    $"redact pass: truncated={truncated}");',
        'foreach (var span in spans) {\n    if (span.TraceContext == null) {\n        span.TraceContext =\n            TraceContext.Generate();\n    } else {\n        span.TraceContext =\n            parent.ChildContext();\n    }\n    if (!baggage.IsEmpty) {\n        span.TraceContext.Baggage =\n            baggage.Snapshot();\n    }\n    propagator.Inject(\n        span.TraceContext,\n        span.OutgoingHeaders);\n    propagated++;\n}',
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
        // 30 more
        'WARN — rbs disc wear',        'WARN — bci ring2 noise',      'WARN — hkb spring tension',
        'WARN — enclave attestation',  'WARN — glmz node lag',        'WARN — corp sync drift',
        'WARN — token near expiry',    'WARN — relay packet loss',    'WARN — db write amplify',
        'WARN — file descriptor high', 'WARN — cpu throttle 88°C',    'WARN — swap pressure',
        'WARN — tls cert chain depth', 'WARN — bci motor drift',      'WARN — dark-node latency',
        'WARN — audit log behind',     'WARN — snapshot stale 48h',   'WARN — license near limit',
        'WARN — rng entropy 32bit',    'WARN — bci power 12%',        'WARN — ipc queue depth',
        'WARN — hkb cycle count high', 'WARN — glmz enforcement near','WARN — corp data horizon',
        'NOTICE — corp-enclave query', 'NOTICE — off-site login',     'NOTICE — bci key age 180d',
        'NOTICE — rate limit 90%',     'NOTICE — new district node',  'NOTICE — freelancer blacklist',
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

    var tickTimer = null;

    function rand(a, b) { return Math.floor(Math.random() * (b - a + 1)) + a; }
    function pick(arr)  { return arr[rand(0, arr.length - 1)]; }
    function getHost()  { return document.querySelector('.console-bg-host'); }

    // Cyberpunk neon-flicker on entity tabs — invoked from EntityDetail.razor after the
    // entity loads. ~25% of the visible tabs briefly flicker like a damaged neon sign,
    // then settle. Represents urban decay, not a UI bug.
    window.cyberspaceEntityFlickerTabs = function () {
        var tabs = document.querySelectorAll('.entity-tabs .nav-link');
        if (!tabs.length) return;
        for (var i = 0; i < tabs.length; i++) {
            if (Math.random() < 0.25) {
                (function (el) {
                    el.classList.remove('entity-tab-flicker');
                    void el.offsetHeight; // force restart if class was already applied
                    el.classList.add('entity-tab-flicker');
                    setTimeout(function () { el.classList.remove('entity-tab-flicker'); }, 1700);
                })(tabs[i]);
            }
        }
    };

    // Continuous neon flicker on dictionary list items. Picks ~25% of visible
    // .list-item rows in any .dict-items container and loops the broken-neon
    // animation on them indefinitely. Clicking a flickering item REMOVES the
    // class — the item "fixes itself" on selection, the rest keep glitching.
    // Idempotent: per-item dataset flag prevents re-randomising touched items
    // (so a clicked-fixed item stays fixed even when Blazor re-renders the list,
    // and an actively-flickering item doesn't restart its animation cycle).
    //
    // DISABLED 2026-05-09 — kept for later re-enablement. Function is callable
    // but no caller invokes it; MainLayout's invocation site is commented out.
    // To restore, uncomment the call in MainLayout.OnAfterRenderAsync.
    window.cyberspaceStartListItemFlicker = function () {
        function apply() {
            var items = document.querySelectorAll('.dict-items .list-item');
            for (var i = 0; i < items.length; i++) {
                var el = items[i];
                if (el.dataset.flickerSet === '1') continue;
                el.dataset.flickerSet = '1';
                if (Math.random() < 0.25) el.classList.add('neon-flicker-loop');
            }
        }
        if (window.__cyberspaceListFlickerInited) { apply(); return; }
        window.__cyberspaceListFlickerInited = true;

        // One delegated click handler covers every .dict-items list — Blazor
        // re-renders rows but the document-level listener stays bound for the
        // lifetime of the page.
        document.addEventListener('click', function (e) {
            var item = e.target && e.target.closest && e.target.closest('.dict-items .list-item');
            if (item) {
                item.classList.remove('neon-flicker-loop');
                item.dataset.flickerSet = 'fixed';
            }
        });

        apply();
        // Re-apply periodically so newly-rendered rows (after a filter / save /
        // page-switch) get their share of broken neon.
        setInterval(apply, 2500);
    };

    // Returns the bottom edge of .board-grid as a % of viewport height, +2% buffer.
    // Spawn positions use this as their minimum top value so nothing overlaps tiles.
    // randTop: unrestricted — effects can spawn anywhere including over tiles
    function randTop(lo, hi) { return rand(lo, hi); }

    // ── Effect keepout zones — rects no effect should spawn behind ───────────────
    // Computed live each call so it adapts to resizes / responsive layout changes.
    //
    // Baked-in selectors (any host gets these for free):
    //   .cyberspace-keepout     — opt-in marker; add to any container you want protected
    //   main             — top-level page content (mindattic.com #content lives here)
    //   .home-content    — StreetSamurai Home page wrapper
    //   .board-grid      — any tab/tile board
    //
    // Hosts can extend the list via window.__cyberspaceKeepoutSelectors (CSS selector
    // string, comma-separated).
    function getKeepoutRects() {
        var extra = (typeof window !== 'undefined' && window.__cyberspaceKeepoutSelectors) || '';
        var selector = '.cyberspace-keepout, main, .home-content, .board-grid' + (extra ? ', ' + extra : '');
        var sel = document.querySelectorAll(selector);
        var rects = [];
        for (var i = 0; i < sel.length; i++) {
            var r = sel[i].getBoundingClientRect();
            if (r.width > 8 && r.height > 8) rects.push(r);
        }
        return rects;
    }

    // Try several random positions, pick the one with the least overlap against existing large
    // overlays AND the tile keepout rect. estW/estH are pixel estimates of the element. Tile
    // overlap is weighted heavily so the placer avoids it unless every option is bad.
    function bestPos(host, estW, estH, xMin, xMax, yMin, yMax) {
        var hw = window.innerWidth, hh = window.innerHeight;
        var sel = host.querySelectorAll('.cyberspace-win,.cyberspace-err-popup,.cyberspace-warn-popup,.cyberspace-memo');
        var rects = [];
        for (var i = 0; i < sel.length; i++) rects.push(sel[i].getBoundingClientRect());
        var keepout = getKeepoutRects();
        var bx = rand(xMin, xMax), by = rand(yMin, yMax), bov = Infinity;
        for (var t = 0; t < 8; t++) {
            var cx = rand(xMin, xMax), cy = rand(yMin, yMax);
            var px = (cx / 100) * hw, py = (cy / 100) * hh;
            var ov = 0;
            for (var r = 0; r < rects.length; r++) {
                var rc = rects[r];
                var ox = Math.max(0, Math.min(px + estW, rc.right)  - Math.max(px, rc.left));
                var oy = Math.max(0, Math.min(py + estH, rc.bottom) - Math.max(py, rc.top));
                ov += ox * oy;
            }
            // Tile keepout — weighted 4× so a partial keepout overlap is worse than a window overlap
            for (var k = 0; k < keepout.length; k++) {
                var kc = keepout[k];
                var kox = Math.max(0, Math.min(px + estW, kc.right)  - Math.max(px, kc.left));
                var koy = Math.max(0, Math.min(py + estH, kc.bottom) - Math.max(py, kc.top));
                ov += kox * koy * 4;
            }
            if (ov < bov) { bov = ov; bx = cx; by = cy; }
            if (ov === 0) return [cx, cy]; // perfect — no overlap, exit early
        }
        return [bx, by];
    }

    // Pick a random viewport-% position for a small effect (artifact / fragment / morse-dot)
    // that doesn't fall inside the tile keepout zone. estW/estH are pixel estimates. If no
    // clean spot is found in `tries` attempts, returns the least-overlapping candidate.
    function safePos(estW, estH, xMin, xMax, yMin, yMax, tries) {
        tries = tries || 10;
        var hw = window.innerWidth, hh = window.innerHeight;
        var keepout = getKeepoutRects();
        if (keepout.length === 0) return [rand(xMin, xMax), rand(yMin, yMax)];
        var bx = rand(xMin, xMax), by = rand(yMin, yMax), bov = Infinity;
        for (var t = 0; t < tries; t++) {
            var cx = rand(xMin, xMax), cy = rand(yMin, yMax);
            var px = (cx / 100) * hw, py = (cy / 100) * hh;
            var ov = 0;
            for (var k = 0; k < keepout.length; k++) {
                var kc = keepout[k];
                var ox = Math.max(0, Math.min(px + estW, kc.right)  - Math.max(px, kc.left));
                var oy = Math.max(0, Math.min(py + estH, kc.bottom) - Math.max(py, kc.top));
                ov += ox * oy;
            }
            if (ov < bov) { bov = ov; bx = cx; by = cy; }
            if (ov === 0) return [cx, cy];
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
            var colorVar = pick(['', '', 'cyberspace-win--blue', 'cyberspace-win--amber']);
            win.className = 'cyberspace-win' + (colorVar ? ' ' + colorVar : '') + (extraClass ? ' ' + extraClass : '');
            var wp = posX !== undefined ? [posX, posY] : bestPos(host, 228, 140, -8, 88, 4, 76);
            win.style.left = wp[0] + '%';
            win.style.top  = wp[1] + '%';

            var titleEl = document.createElement('div');
            titleEl.className = 'cyberspace-title';
            titleEl.textContent = pick(TITLES);
            win.appendChild(titleEl);

            var body = document.createElement('div');
            body.className = 'cyberspace-body';
            win.appendChild(body);

            host.appendChild(win);

            var lineCount = rand(2, 6);
            var lines = [];
            for (var i = 0; i < lineCount; i++) { lines.push(pick(LINES)); }

            var success = !waiting && Math.random() > 0.32;
            var result  = pick(success ? OK_RESULTS : ERR_RESULTS);
            var resCls  = success ? 'cyberspace-ok' : 'cyberspace-err';

            var idx = 0;
            var lineTimer = setInterval(function () {
                if (idx < lines.length) {
                    var ln = document.createElement('div');
                    ln.className = 'cyberspace-line';
                    ln.textContent = lines[idx];
                    body.appendChild(ln);
                    idx++;
                } else {
                    clearInterval(lineTimer);
                    setTimeout(function () {
                        if (waiting) {
                            // Show a prompt line with blinking cursor — no result, lingers
                            var prompt = document.createElement('div');
                            prompt.className = 'cyberspace-line cyberspace-prompt';
                            prompt.innerHTML = '> <span class="cyberspace-cursor">&#9608;</span>';
                            body.appendChild(prompt);
                            // Long random TTL: 6–18 seconds
                            setTimeout(function () {
                                win.classList.add('cyberspace-win--out');
                                setTimeout(function () {
                                    if (win.parentNode) win.parentNode.removeChild(win);
                                }, 160);
                            }, rand(6000, 18000));
                        } else {
                            var res = document.createElement('div');
                            res.className = 'cyberspace-line ' + resCls;
                            res.textContent = result;
                            body.appendChild(res);

                            setTimeout(function () {
                                win.classList.add('cyberspace-win--out');
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
        var stepX = rand(2, 5), stepY = rand(1, 4);
        for (var i = 0; i < n; i++) {
            spawnWindow(i * rand(65, 210), x + i * stepX, y + i * stepY, 'cyberspace-cascade');
        }
    }

    // ── Fatal error popup ───────────────────────────────────────────────────

    function spawnError(posX, posY) {
        var host = getHost();
        if (!host) return;

        var popup = document.createElement('div');
        popup.className = 'cyberspace-err-popup';
        var ep = posX !== undefined ? [posX, posY] : bestPos(host, 310, 90, -5, 80, 5, 88);
        popup.style.left = ep[0] + '%';
        popup.style.top  = ep[1] + '%';

        // Layout: [red icon] | [title \n message \n ... \n OK btn]
        var icon = document.createElement('div');
        icon.className = 'cyberspace-err-popup-icon';
        icon.textContent = '⬛';   // replaced visually via CSS — just a block placeholder
        popup.appendChild(icon);

        var content = document.createElement('div');
        content.className = 'cyberspace-err-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cyberspace-err-popup-title';
        titleEl.textContent = pick(FATAL_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cyberspace-err-popup-msg';
        msgEl.textContent = pick(FATAL_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);

        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cyberspace-win--out');
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
        popup.className = 'cyberspace-warn-popup';
        var wp2 = posX !== undefined ? [posX, posY] : bestPos(host, 290, 90, -5, 82, 5, 88);
        popup.style.left = wp2[0] + '%';
        popup.style.top  = wp2[1] + '%';

        var content = document.createElement('div');
        content.className = 'cyberspace-warn-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cyberspace-warn-popup-title';
        titleEl.textContent = pick(WARN_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cyberspace-warn-popup-msg';
        msgEl.textContent = pick(WARN_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);
        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cyberspace-win--out');
            setTimeout(function () {
                if (popup.parentNode) popup.parentNode.removeChild(popup);
            }, 160);
        }, rand(2500, 5500));
    }

    // ── Floating code fragments ─────────────────────────────────────────────

    // Fragment color variants — additive classes; default (no extra class) is green.
    var FRAG_COLOR_VARIANTS = ['', 'cyberspace-frag--cyan', 'cyberspace-frag--amber', 'cyberspace-frag--magenta', 'cyberspace-frag--white'];
    // Plausible typo characters — adjacent-key feel plus common punctuation.
    var FRAG_TYPO_CHARS = 'abcdefghijklmnopqrstuvwxyz_-=+*/.,;:()[]{}';

    function spawnFrag() {
        var host = getHost();
        if (!host) return;

        var el = document.createElement('div');
        var variant = pick(FRAG_COLOR_VARIANTS);
        el.className = 'cyberspace-frag' + (variant ? ' ' + variant : '');
        // Code fragments are ~280×120 estimated — pick a position that avoids the tile keepout
        var fp = safePos(280, 120, -4, 94, 2, 94);
        el.style.left = fp[0] + '%';
        el.style.top  = fp[1] + '%';
        host.appendChild(el);

        var text      = pick(FRAGS);
        // msPerChar 1.1-3.3 — 10% slower than the previous rand(1,3).
        var msPerChar = rand(11, 33) / 10;
        var lastTs    = null;

        // Build the typing plan as a list of segments. Normal frags are one
        // straight append; ~30% of long-enough frags get a typo + pause + backspace
        // + resume sequence, simulating a fumble.
        var segs = [];
        if (text.length > 12 && Math.random() < 0.30) {
            var mistakeAt  = rand(6, text.length - 6);
            var garbageLen = rand(2, 5);
            var garbage    = '';
            for (var g = 0; g < garbageLen; g++) {
                garbage += FRAG_TYPO_CHARS[Math.floor(Math.random() * FRAG_TYPO_CHARS.length)];
            }
            segs.push({ kind: 'type', str: text.slice(0, mistakeAt) });
            segs.push({ kind: 'type', str: garbage });
            segs.push({ kind: 'pause', ms: rand(180, 360) });
            segs.push({ kind: 'back', count: garbageLen });
            segs.push({ kind: 'type', str: text.slice(mistakeAt) });
        } else {
            segs.push({ kind: 'type', str: text });
        }

        var segIdx    = 0;   // current segment index
        var segDone   = 0;   // chars completed within current segment (type/back) or 0 (pause)
        var pauseEnd  = 0;   // ts at which current pause ends (0 = pause not yet entered)
        var content   = '';  // accumulated visible text (incl. transient garbage)

        // Drive typing on rAF rather than a 1-3ms setInterval. Background tabs
        // throttle setTimeout/setInterval to ~1Hz (Chrome intensive throttling can
        // stretch that to a minute+), which used to leave fragments stuck mid-type
        // for many seconds while the tick loop kept spawning new ones. rAF is fully
        // paused while the tab is hidden, so typing simply resumes when you return.
        function frame(ts) {
            if (lastTs == null) lastTs = ts;
            var dt = ts - lastTs;
            lastTs = ts;
            var charsAvail = Math.max(1, Math.floor(dt / msPerChar));

            while (charsAvail > 0 && segIdx < segs.length) {
                var s = segs[segIdx];
                if (s.kind === 'type') {
                    var take = Math.min(charsAvail, s.str.length - segDone);
                    content += s.str.slice(segDone, segDone + take);
                    segDone += take;
                    charsAvail -= take;
                    if (segDone >= s.str.length) { segIdx++; segDone = 0; }
                } else if (s.kind === 'back') {
                    var take2 = Math.min(charsAvail, s.count - segDone);
                    content = content.slice(0, content.length - take2);
                    segDone += take2;
                    charsAvail -= take2;
                    if (segDone >= s.count) { segIdx++; segDone = 0; }
                } else { // pause
                    if (pauseEnd === 0) pauseEnd = ts + s.ms;
                    if (ts >= pauseEnd) { segIdx++; pauseEnd = 0; }
                    break;
                }
            }

            el.textContent = content;

            if (segIdx >= segs.length) {
                setTimeout(function () {
                    el.classList.add('cyberspace-frag--out');
                    setTimeout(function () {
                        if (!el.__predated && el.parentNode) el.parentNode.removeChild(el);
                    }, 360);
                }, rand(18, 75));
                return;
            }
            requestAnimationFrame(frame);
        }
        requestAnimationFrame(frame);
    }

    // ── Geometry schematic window ────────────────────────────────────────────
    // Shape catalog + renderer extracted to Components/SacredGeometry/sacred-geometry.js
    // (window.SacredGeometry). spawnGeoWindow() below draws into it; nothing else changed.

    function spawnGeoWindow(forceIdx) {
        var host = getHost();
        if (!host) return;
        var SG = window.SacredGeometry;
        if (!SG) return;   // graceful degrade if sacred-geometry.js didn't load
        var idx = (typeof forceIdx === 'number') ? forceIdx : rand(0, SG.count - 1);

        var win = document.createElement('div');
        win.className = 'cyberspace-win cyberspace-geo-win';
        var gp = bestPos(host, 240, 130, -5, 80, 5, 90);
        win.style.left = gp[0] + '%';
        win.style.top  = gp[1] + '%';

        var titleEl = document.createElement('div');
        titleEl.className = 'cyberspace-title';
        titleEl.textContent = 'schematic/' + SG.label(idx);
        win.appendChild(titleEl);

        var S = 110;
        var bodyEl = document.createElement('div');
        bodyEl.className = 'cyberspace-geo-body';
        var cv = document.createElement('canvas');
        cv.className = 'cyberspace-geo-canvas';
        cv.width = S; cv.height = S;
        bodyEl.appendChild(cv);
        win.appendChild(bodyEl);
        host.appendChild(win);

        var ctx2 = cv.getContext('2d');
        var angle = Math.random() * Math.PI * 2;
        var rafId = null;

        // proj3 + all wire/parametric draw routines now live in window.SacredGeometry.
        function geoFrame() {
            ctx2.clearRect(0, 0, S, S);
            angle += 0.007;   // SacredGeometry.draw handles its own centering transform
            SG.draw(ctx2, idx, angle, {
                size: S,
                stroke: 'rgba(70,210,170,0.55)', dotColor: 'rgba(70,210,170,0.85)',
                lineWidth: 0.7, dotRadius: 1.2
            });
            rafId = requestAnimationFrame(geoFrame);
        }
        geoFrame();

        // ── Scrolling element report text (right of canvas) ────────────────
        var reportEl = document.createElement('div');
        reportEl.className = 'cyberspace-geo-report';
        var innerEl = document.createElement('div');
        innerEl.className = 'cyberspace-geo-report-inner';
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
            win.classList.add('cyberspace-win--out');
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
        // ── 10 additional leaked memos ─────────────────────────────────────
        'FROM: Vultures Procurement\nTO: Corp Medical Logistics\nRE: Q4 Body Recovery — Pricing\n\n441 bodies processed Q4.\nUsable organ yield: 88% — record high.\nResale: Φ12.4M to corp medical.\nFamilies notified — generic causes.\n\n>>> INTERCEPT :: dist7/vault <<<',
        'CONFIDENTIAL — DELETE ON READ\nFROM: District 4 Sponsorship Office\nTO: Corp Tier Management\nRE: Tier Demotion Protocol — Update\n\n3,301 tier-3 holders flagged Q4.\nDemotion to tier-2: 88% — quota met.\nPublic ceremony required — 5 weekly.\nMaintain humiliation visibility.\n\n[SIG: 0x9A3301 // SPONSORSHIP]',
        'FROM: Libation Corp Senior Staff\nTO: Crest Dynamics Liaison\nRE: Joint Tier Mobility Pilot — Q1\n\nPilot: 12 sponsorships across corps.\nTier-1 to tier-3 advancement: 4 cases.\nPress narrative: meritocratic success.\nOmit: 8 demotions to tier-1 same period.\n\n>>> CORP/BROKER :: VAULTDROP <<<',
        'INTERNAL — NO EXTERNAL\nFROM: Arcturus Civil Security\nTO: GLMZ Coordination Office\nRE: Police Vacuum — Q4 Brief\n\nNo official GLMZ police force exists.\nArcturus fills 88% of enforcement role.\nPress: never call us "the police."\nWe are private contractors. Always.\n\n[INTERCEPT: glmz/comms-d12]',
        'FROM: Behemoth Operations Group\nTO: Corp Board — Eyes Only\nRE: Iowan Behemoth Update — Q4\n\n12 Iowan units active GLMZ-perimeter.\nAutonomous decisions: 99.7%.\nThey are NOT alive — repeat: NOT alive.\nIf press asks, they are "smart machines."\n\n[SIG: 0xFF0000 // BOARD-ONLY]',
        'TO: Corp Board\nFROM: Pulse Network Operations\nRE: Ferrogate Mach-6 Disruption\n\nDistrict 9 → Rotterdam pod sabotaged.\n44 fatalities — frame as mechanical.\nFerrogate liability: shielded by NDA.\nVictims: all dist9 freelancer-adjacent.\n\n>>> DARK NODE :: pulse/operations <<<',
        'FROM: Free Floating Coordination\nTO: Legal Loophole Group\nRE: Michigan City Casino — Status\n\nDrowned hull still classified vehicle.\nGLMZ jurisdiction: void by sea-treaty.\n88 freelancers operating from hull.\nDo not interfere — sets bad precedent.\n\n[INTERCEPT CONFIDENCE: 0.92]',
        'CLASSIFIED — DESTROY AFTER\nFROM: Cortical Compliance Audit\nTO: Crest Board\nRE: Φ Currency Cortical Bind\n\n441,000 operators have BCI-bound Φ.\nWallet-spoofing: cortical impossible.\nBenefit: zero theft. Risk: zero opt-out.\nDo not brief regulators on this fact.\n\n[SIG: 0x9F3A // CORP-LEGAL]',
        'FROM: Reliquary Branch — Outreach\nTO: Corp Cultural Liaison\nRE: Pixel Asset — Recovery\n\nPixel artifact ("notebook") still missing.\nOriginal owner: Auntie Hoa, dist7.\nReliquary will pay Φ440,000 — discreet.\nDo NOT involve corp enforcement.\n\n>>> RELAY: dist7/comms <<<',
        'PRIORITY ALPHA — BOARD ONLY\nFROM: Crest Existential Risk Cell\nTO: Corp Board (Eyes Only)\nRE: Sasha Võ — Operational Status\n\n4471-K confirmed active across districts.\nFormer alias "Lena Connor" — fully retired.\nOperator is fully trained, not savant.\nDo NOT classify as cliché — strategic.\n\n[SIG: 0xFF0000 // EXECUTIVE-ONLY]',
    ];

    // ── Memo erasure — JS character-by-character erase ──────────────────────
    function eraseMemo(el) {
        var BLOCK = '█▓▒░▪■▫';
        var chars = el.textContent.split('');
        var total = chars.length;
        var phase = 0; // 0 = scramble with blocks, 1 = clear to spaces
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
                el.classList.add('cyberspace-memo--collapse');
                setTimeout(function () {
                    if (!el.__predated && el.parentNode) el.parentNode.removeChild(el);
                }, 360);
            }
        }, 22);
    }

    function spawnMemo() {
        var host = getHost();
        if (!host) return;
        var el = document.createElement('div');
        el.className = 'cyberspace-memo';
        var mp = bestPos(host, 240, 110, -5, 78, 4, 88);
        el.style.left = mp[0] + '%';
        el.style.top  = mp[1] + '%';
        el.textContent = pick(MEMOS);
        host.appendChild(el);
        setTimeout(function () { eraseMemo(el); }, rand(3000, 6000));
    }


    // ── Scrolling texture layer ─────────────────────────────────────────────

    // Hosts can override these by defining window.__cyberspaceCircuitboardSrcs = [a, b, c]
    // before this script loads. mindattic.com sets them to pinned jsDelivr URLs;
    // StreetSamurai leaves the default (/api/media/... served by MediaController).
    var TEX_SRCS = (typeof window !== 'undefined' && window.__cyberspaceCircuitboardSrcs) || [
        '/api/media/circuitboard.00.png',
        '/api/media/circuitboard.01.png',
        '/api/media/circuitboard.02.png',
    ];
    var texLayers = null;   // built once; images reused across navigations
    var texRaf    = null;
    var texTimer  = null;
    var texResizeBound = false;
    var texCanvas = null;
    var texRO     = null;

    function syncTexCanvasSize() {
        if (!texCanvas) return;
        // Use the live viewport — the host is position:fixed;inset:0 so it always
        // matches window inner dimensions. Round in case devicePixelRatio gives
        // fractional pixels.
        var w = Math.max(1, Math.round(window.innerWidth));
        var h = Math.max(1, Math.round(window.innerHeight));
        if (texCanvas.width  !== w) texCanvas.width  = w;
        if (texCanvas.height !== h) texCanvas.height = h;
    }

    function initTextures() {
        var host = getHost();
        if (!host) return;

        // Reuse or create canvas inside host. CSS width/height:100% forces the
        // canvas's CSS box to stretch with the host regardless of the width/height
        // HTML attributes (which would otherwise also dictate the CSS box size
        // and leave new viewport area uncovered after a maximize/resize).
        var canvas = host.querySelector('.cyberspace-tex');
        if (!canvas) {
            canvas = document.createElement('canvas');
            canvas.className = 'cyberspace-tex';
            canvas.style.cssText = 'position:absolute;left:0;top:0;width:100%;height:100%;pointer-events:none;z-index:0;';
            host.insertBefore(canvas, host.firstChild);
        } else {
            canvas.style.cssText = 'position:absolute;left:0;top:0;width:100%;height:100%;pointer-events:none;z-index:0;';
        }
        texCanvas = canvas;
        syncTexCanvasSize();
        var ctx = canvas.getContext('2d');

        // Keep the bitmap in sync with the viewport — without this the parallax
        // texture only paints the original window area after a maximize/resize.
        if (!texResizeBound) {
            window.addEventListener('resize', syncTexCanvasSize);
            texResizeBound = true;
        }
        // (Re)point the ResizeObserver at the *current* host so zoom changes,
        // devtools panel toggles, and other container-size shifts repaint
        // correctly. On Blazor navigation the old .console-bg-host is detached
        // and a fresh one inserted; without re-observing here the observer would
        // stay bound to the dead node and never fire for the live host.
        if (typeof ResizeObserver !== 'undefined') {
            if (!texRO) texRO = new ResizeObserver(syncTexCanvasSize);
            else texRO.disconnect();
            texRO.observe(host);
        }

        // Build layer descriptors once; images survive page navigations
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

        // Direction randomiser — shift one layer's heading every 3-7s
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

        // Restart animation loop on the (possibly new) canvas
        if (texRaf) cancelAnimationFrame(texRaf);

        function frame() {
            // Read live so a resize-driven canvas.width/height change takes effect immediately.
            var w = canvas.width, h = canvas.height;
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

    // ── Folder-rip effect — file browser with selection getting yanked away ─────
    // Filenames pool — cyberpunk-flavored: corp dossiers, neural batches, classified ops
    var FOLDER_PATHS = [
        '/vault/crest', '/dist04/ops', '/vault/0x9F3A', '/sec/bci', '/dist09/audit',
        '/relay/dark', '/glmz/ferro', '/legal/redact', '/comms/intercept', '/dist12/cls',
        '/sec/enclave', '/data/neural', '/finance/slush', '/audit/burn', '/corp/board',
        '/glmz/water', '/medical/ethics', '/personnel/restrict', '/territory/buyout', '/vault/0x3AF7'
    ];
    var FOLDER_NAMES = [
        'crest_keys.dat', 'bci_dump_q3', 'batch_44_neural', 'protocol_9.exe', 'vault_0x9F3A',
        'meridian88_logs', 'silent_horizon', 'subject_3301', 'cohort_14_med', 'op_blackout',
        'witness_0091', 'dark_census_l3', 'profile_pkg_q4', 'redaction_q3.tar', 'narrative_phase5',
        'relay_map_d12', 'reroute_0xBE', 'compliance_a8', 'pulse_seg_44', 'sponsorship_demo',
        'silent_witness', 'tier3_demote', 'ferrogate_log', 'audit_strip_q3', 'cohort_failed',
        'consent_revoke', 'pop_throttle_d9', 'enclave_seed', 'leak_inside_07', 'class_action_88',
        'autonomy_lock', 'kill_switch.dat', 'org_dissolve_d2', 'price_floor_x4', 'rev_q3.csv',
        'subject_x4471k', 'med_diversion', 'legal_stratagem', 'vault_drop_4412', 'override_disable'
    ];
    function folderItemText() { return pick(FOLDER_NAMES); }

    function spawnFolderRip() {
        var host = getHost();
        if (!host) return;
        var win = document.createElement('div');
        win.className = 'cyberspace-win cyberspace-folder-win';
        var fp = bestPos(host, 220, 170, -2, 86, 4, 80);
        win.style.left = fp[0] + '%';
        win.style.top  = fp[1] + '%';

        var titleEl = document.createElement('div');
        titleEl.className = 'cyberspace-title';
        titleEl.textContent = 'filebrowser' + pick(FOLDER_PATHS);
        win.appendChild(titleEl);

        var listEl = document.createElement('div');
        listEl.className = 'cyberspace-folder-list';
        var n = rand(6, 10);
        var folders = [];
        for (var i = 0; i < n; i++) {
            var f = document.createElement('div');
            f.className = 'cyberspace-folder';
            f.textContent = '► ' + folderItemText();
            listEl.appendChild(f);
            folders.push(f);
        }
        win.appendChild(listEl);
        host.appendChild(win);

        // After a beat, drop the highlight on a random consecutive run of 1–3 folders.
        var hiCount = rand(1, 3);
        var hiStart = Math.floor(Math.random() * (folders.length - hiCount + 1));
        setTimeout(function () {
            for (var i = hiStart; i < hiStart + hiCount; i++) folders[i].classList.add('cyberspace-folder-highlight');
        }, 500);

        // Extract the highlighted items — each file does ONE simple slide-blink-fade. The
        // staggering is BETWEEN files: file 1 leaves first, file 2 starts 180ms later, file 3
        // another 180ms later, and so on. Each individual file's motion is simple and clean.
        // Tight stagger — files stay packed close together as they exit. 40ms between files
        // means file 4 starts when file 1 has only moved ~6% of the way; the selection reads
        // as a coherent stack sliding out together rather than spread-out individuals.
        var ripStartMs   = 1100;
        var perFileDelay = 40;
        var perFileDur   = 640;
        setTimeout(function () {
            var ripDist = 240; // > window width so the folder clears the right edge entirely
            for (var i = hiStart; i < hiStart + hiCount; i++) {
                var f = folders[i];
                f.classList.add('cyberspace-folder-ripping'); // lift z-index above the window
                f.style.setProperty('--rx', ripDist + 'px');
                f.style.setProperty('--ry', '0px');
                var delay = (i - hiStart) * perFileDelay;
                f.style.animation = 'cyberspace-folder-rip ' + perFileDur + 'ms cubic-bezier(0.32, 0.72, 0, 1) ' + delay + 'ms forwards';
            }
        }, ripStartMs);

        // Window fades out exactly when the steal completes — tied to the LAST file's animation
        // end. Last file leaves at: ripStart + (hiCount-1)*perFileDelay + perFileDur, plus a small
        // beat so the empty window registers visually before it dissolves.
        var stealCompleteMs = ripStartMs + (hiCount - 1) * perFileDelay + perFileDur;
        var winFadeAt = stealCompleteMs + 220;
        setTimeout(function () {
            // The window still has cyberspace-in's animation-fill-mode:forwards holding
            // opacity 0.76; a transition can't reliably override that. Apply a
            // keyframe animation instead — fresh animation wins over the prior
            // forwards fill, so the fade always resolves visually.
            win.classList.add('cyberspace-folder-win--fade');
            setTimeout(function () { if (win.parentNode) win.parentNode.removeChild(win); }, 600);
        }, winFadeAt);
    }

    // ── Floating artifact clusters ───────────────────────────────────────────

    // Fibonacci + golden-ratio constants — every artifact sizing/timing decision draws from these
    var ART_PHI       = 1.6180339887;             // golden ratio φ
    var ART_INV_PHI   = 0.6180339887;             // 1/φ — the "minor" partition of golden division
    var ART_FIB_DIMS  = [5, 8, 13];               // grid row/column counts (Fibonacci numbers)
    var ART_FIB_SIZES = [8, 13, 21];              // per-row / per-col pixel sizes (consecutive Fibs → neighbors sit at golden ratio)
    var ART_DIGITS    = '0123456789';

    // Predator wasps use #ff2244 (bright red). ARTIFACT cells stay off that hue so the
    // consume-and-convert flash is visually unambiguous when a swarm shows up.
    var ART_PALETTES = ['cyberspace-artifact--white', 'cyberspace-artifact--blue', 'cyberspace-artifact--amber'];

    // ARTIFACT variant enum — each spawn picks one. Names listed in the registry at the top
    // of this file; each is a preserved iteration of how ARTIFACTs have looked.
    //   SCATTER    — original random blob, all glyphs drift in one direction
    //   LATTICE    — Fibonacci grid, whole lattice drifts together (corner-wave delay)
    //   ANCHOR     — stationary grid; glitches in place; leading edge emits feelers
    //   SLUG       — single grid crawls + per-cell undulation
    //   CENTIPEDE  — multi-segment chain, peristaltic wave + head feelers
    //   PULSE      — concentric Fibonacci rings, lub-dub heartbeat (simulates life)
    //   WANDERER   — small grid walks + pauses to "look around" (simulates awareness)
    //   SPIDER     — 8-leg radial cluster, alternate-pulse walking gait
    //   INCHWORM   — chain bunches and extends; translation only during the extend phase
    //   HOPPER     — cluster crouch + parabolic launch + land cycle (grasshopper)
    //   JELLY      — concentric rings pulse outward/in; vertical drift
    //   SQUID      — pulse-jet propulsion: body squash + tentacles trail behind
    //   BEETLE     — scurry-stop-scurry with shell shimmer + small antennae
    //   TADPOLE    — small body + whipping sinusoidal tail; tail strokes propel body
    //   ANT        — zigzag walk with vibrating antennae at the head
    //   ABSTRACT GEOMETRIC / PHYSICAL VARIANTS (no biological metaphor):
    //   MOIRE      — two overlapping rotated grids producing interference
    //   VORTEX     — spiral inflow; particles drift along an Archimedean spiral
    //   EXPLOSION  — outward burst with deceleration to a slow float
    //   ORBIT      — concentric rings rotating at golden-ratio period differences
    //   NETWORK    — nodes float on per-node axes; multi-glyph packets travel between them
    //   PHASEFIELD — wide grid; cell brightness phases by a traveling (x+y) wave
    //   SHATTER    — cluster fractures into N pieces that slowly drift apart
    //   SPIROGRAM  — glyphs trail along an epitrochoid (Spirograph) curve
    //   PULSARRING — single ring with brightness wave traveling around its circumference
    var ART_VARIANTS = ['SCATTER', 'LATTICE', 'ANCHOR', 'SLUG', 'CENTIPEDE', 'PULSE', 'WANDERER',
        'SPIDER', 'INCHWORM', 'HOPPER',
        'JELLY', 'SQUID', 'BEETLE', 'TADPOLE', 'ANT',
        'MOIRE', 'VORTEX', 'EXPLOSION', 'ORBIT', 'NETWORK',
        'PHASEFIELD', 'SHATTER', 'SPIROGRAM', 'PULSARRING'];

    // Shared helper — create a Fibonacci-sized grid scaffold (variable col widths / row heights).
    // Returns positions + cumulative offsets so callers can build cells.
    function artBuildFibGrid(cols, rows) {
        var colW = new Array(cols), rowH = new Array(rows);
        for (var c = 0; c < cols; c++) colW[c] = ART_FIB_SIZES[Math.floor(Math.random() * ART_FIB_SIZES.length)];
        for (var r = 0; r < rows; r++) rowH[r] = ART_FIB_SIZES[Math.floor(Math.random() * ART_FIB_SIZES.length)];
        var colX = new Array(cols + 1); colX[0] = 0;
        for (var ci = 0; ci < cols; ci++) colX[ci + 1] = colX[ci] + colW[ci];
        var rowY = new Array(rows + 1); rowY[0] = 0;
        for (var ri = 0; ri < rows; ri++) rowY[ri + 1] = rowY[ri] + rowH[ri];
        return { colW: colW, rowH: rowH, colX: colX, rowY: rowY, totalW: colX[cols], totalH: rowY[rows] };
    }
    function artNewContainer() {
        var el = document.createElement('div');
        el.className = 'cyberspace-artifact ' + pick(ART_PALETTES);
        // Artifact body fits in roughly a 200×140 box once segments and feelers are spawned.
        // Use safePos so the structure doesn't materialise on top of the tile container.
        var ap = safePos(200, 140, -2, 88, -2, 85);
        el.style.left = ap[0] + '%';
        el.style.top  = ap[1] + '%';
        return el;
    }
    function artGlyph() {
        return Math.random() < ART_INV_PHI
            ? ART_DIGITS[Math.floor(Math.random() * 10)]
            : GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
    }

    // ── Variant: SCATTER — original implementation ──────────────────────────────
    // Random scatter inside a circular region; every glyph drifts toward a shared cluster
    // direction with a small individual angle spread. Cells fade out as they travel.
    function spawnArtifactScatter(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var n = rand(14, 28);
        var blobR = rand(40, 80);
        var clusterAngle = Math.random() * Math.PI * 2;
        for (var i = 0; i < n; i++) {
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            var angle = Math.random() * Math.PI * 2;
            var r     = Math.random() * blobR;
            span.style.left     = Math.round(r * Math.cos(angle)) + 'px';
            span.style.top      = Math.round(r * Math.sin(angle)) + 'px';
            span.style.fontSize = rand(9, 20) + 'px';
            span.style.setProperty('--abl', (0.3 + Math.random() * 0.6).toFixed(1) + 'px');
            var glyphAngle = clusterAngle + (Math.random() - 0.5) * 0.88;
            var glyphDist  = rand(50, 130);
            span.style.setProperty('--adx1', Math.round(Math.cos(glyphAngle) * glyphDist) + 'px');
            span.style.setProperty('--ady1', Math.round(Math.sin(glyphAngle) * glyphDist) + 'px');
            var driftDur   = (1.8 + Math.random() * 2.2).toFixed(2);
            var driftDelay = (Math.random() * 1.8).toFixed(2);
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite, cyberspace-art-drift ' + driftDur + 's ease-in ' + driftDelay + 's forwards';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var ttl = rand(6000, 8000);
        setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, ttl);
    }

    // ── Variant: LATTICE — Fibonacci grid, whole lattice drifts together ────────
    // Every cell shares the cluster drift; a corner-anchored ripple delays each cell so the
    // grid moves like a single body, with the ripple reading as wave propagation.
    function spawnArtifactLattice(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var cols = ART_FIB_DIMS[Math.floor(Math.random() * ART_FIB_DIMS.length)];
        var rows = ART_FIB_DIMS[Math.floor(Math.random() * ART_FIB_DIMS.length)];
        var g = artBuildFibGrid(cols, rows);
        var clusterAngle = Math.random() * Math.PI * 2;
        var clusterDist  = Math.round(89 + (Math.random() - 0.5) * (89 * ART_INV_PHI));
        var dx = Math.round(Math.cos(clusterAngle) * clusterDist);
        var dy = Math.round(Math.sin(clusterAngle) * clusterDist);
        var waveOx = Math.random() < 0.5 ? 0 : cols - 1;
        var waveOy = Math.random() < 0.5 ? 0 : rows - 1;
        var stepDelay = 0.055 * ART_PHI;
        for (var row = 0; row < rows; row++) {
            for (var col = 0; col < cols; col++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = Math.round(g.colX[col] - g.totalW / 2) + 'px';
                span.style.top  = Math.round(g.rowY[row] - g.totalH / 2) + 'px';
                var cellMin = Math.min(g.colW[col], g.rowH[row]);
                span.style.fontSize = Math.max(8, Math.round(cellMin * ART_PHI * ART_INV_PHI)) + 'px';
                span.style.setProperty('--abl', (ART_INV_PHI * 0.5 + Math.random() * ART_INV_PHI * 0.5).toFixed(2) + 'px');
                span.style.setProperty('--adx1', dx + 'px');
                span.style.setProperty('--ady1', dy + 'px');
                var waveDist   = Math.abs(col - waveOx) + Math.abs(row - waveOy);
                var driftDelay = (waveDist * stepDelay + Math.random() * ART_INV_PHI * 0.3).toFixed(2);
                var driftDur   = (ART_PHI + Math.random() * ART_PHI).toFixed(2);
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite, cyberspace-art-drift ' + driftDur + 's ease-in ' + driftDelay + 's forwards';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
            }
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var ttl = Math.round(6765 + (Math.random() - 0.5) * 1597);
        setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, ttl);
    }

    // ── Variant: ANCHOR — anchored grid; glitches in place; leading edge emits feelers ──
    // Core lattice does NOT drift — it flickers + glitches in place. Periodic "feeler" glyphs
    // launch from the leading-edge cells (cells with high projection onto the cluster vector).
    function spawnArtifactAnchor(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var cols = ART_FIB_DIMS[Math.floor(Math.random() * ART_FIB_DIMS.length)];
        var rows = ART_FIB_DIMS[Math.floor(Math.random() * ART_FIB_DIMS.length)];
        var g = artBuildFibGrid(cols, rows);
        var clusterAngle = Math.random() * Math.PI * 2;
        var cosA = Math.cos(clusterAngle), sinA = Math.sin(clusterAngle);
        var spans = [], cellInfo = [];
        for (var row = 0; row < rows; row++) {
            for (var col = 0; col < cols; col++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                var leftPx = Math.round(g.colX[col] - g.totalW / 2);
                var topPx  = Math.round(g.rowY[row] - g.totalH / 2);
                span.style.left = leftPx + 'px';
                span.style.top  = topPx + 'px';
                var cellMin = Math.min(g.colW[col], g.rowH[row]);
                var fontPx  = Math.max(8, Math.round(cellMin * ART_PHI * ART_INV_PHI));
                span.style.fontSize = fontPx + 'px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                spans.push(span);
                var ccx = leftPx + g.colW[col] / 2;
                var ccy = topPx  + g.rowH[row] / 2;
                cellInfo.push({ left: leftPx, top: topPx, fontPx: fontPx, score: ccx * cosA + ccy * sinA });
            }
        }
        host.appendChild(el);
        cellInfo.sort(function (a, b) { return b.score - a.score; });
        var leadingPool = cellInfo.slice(0, Math.max(1, Math.round(cellInfo.length * (1 - ART_INV_PHI))));
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var glitchTimer = setInterval(function () {
            var n = Math.max(2, Math.round(spans.length * Math.pow(ART_INV_PHI, 3)));
            for (var i = 0; i < n; i++) spans[Math.floor(Math.random() * spans.length)].textContent = artGlyph();
        }, Math.round(76 * ART_PHI));
        var feelerTimer = setInterval(function () {
            var seed = leadingPool[Math.floor(Math.random() * leadingPool.length)];
            var feel = document.createElement('span');
            feel.className = 'cyberspace-artifact-char';
            feel.style.left = seed.left + 'px';
            feel.style.top  = seed.top  + 'px';
            feel.style.fontSize = seed.fontPx + 'px';
            feel.style.setProperty('--abl', (0.4 + Math.random() * ART_INV_PHI).toFixed(2) + 'px');
            var fdist  = 55 + Math.random() * 89;
            var spread = (Math.random() - 0.5) * 0.42;
            feel.style.setProperty('--adx1', Math.round(Math.cos(clusterAngle + spread) * fdist) + 'px');
            feel.style.setProperty('--ady1', Math.round(Math.sin(clusterAngle + spread) * fdist) + 'px');
            var fdur = ART_PHI + Math.random() * ART_PHI;
            feel.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite, cyberspace-art-drift ' + fdur.toFixed(2) + 's ease-in 0s forwards';
            feel.textContent = artGlyph();
            el.appendChild(feel);
            setTimeout(function () { if (feel.parentNode) feel.parentNode.removeChild(feel); }, fdur * 1000 + 250);
        }, 89);
        var ttl = Math.round(6765 + (Math.random() - 0.5) * 1597);
        setTimeout(function () {
            clearInterval(glitchTimer); clearInterval(feelerTimer);
            if (!el.__predated && el.parentNode) el.parentNode.removeChild(el);
        }, ttl);
    }

    // ── Variant: SLUG — single grid that crawls + per-cell undulation ──────────
    // The whole grid translates slowly across the screen via cyberspace-artifact-crawl. Per-cell
    // undulation makes the body deform/breathe as it crawls. Disintegrates at end.
    function spawnArtifactSlug(host) {
        var el = artNewContainer();
        var cols = ART_FIB_DIMS[Math.floor(Math.random() * ART_FIB_DIMS.length)];
        var rows = ART_FIB_DIMS[Math.floor(Math.random() * ART_FIB_DIMS.length)];
        var g = artBuildFibGrid(cols, rows);
        var clusterAngle = Math.random() * Math.PI * 2;
        var cosA = Math.cos(clusterAngle), sinA = Math.sin(clusterAngle);
        var crawlDist = 144 + Math.random() * 89;
        el.style.setProperty('--cdx', Math.round(cosA * crawlDist) + 'px');
        el.style.setProperty('--cdy', Math.round(sinA * crawlDist) + 'px');
        el.style.animation = 'cyberspace-artifact-crawl 7.2s ease-in-out forwards';
        var spans = [];
        for (var row = 0; row < rows; row++) {
            for (var col = 0; col < cols; col++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = Math.round(g.colX[col] - g.totalW / 2) + 'px';
                span.style.top  = Math.round(g.rowY[row] - g.totalH / 2) + 'px';
                var cellMin = Math.min(g.colW[col], g.rowH[row]);
                span.style.fontSize = Math.max(8, Math.round(cellMin * ART_PHI * ART_INV_PHI)) + 'px';
                var uAng = Math.random() * Math.PI * 2;
                var uMag = 3 + Math.random() * 4;
                span.style.setProperty('--ux', Math.round(Math.cos(uAng) * uMag) + 'px');
                span.style.setProperty('--uy', Math.round(Math.sin(uAng) * uMag) + 'px');
                var undDur   = (2.0 + Math.random() * 1.6).toFixed(2);
                var undDelay = (-Math.random() * 3).toFixed(2);
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite, '
                                     + 'cyberspace-art-undulate ' + undDur + 's ease-in-out ' + undDelay + 's infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                spans.push(span);
            }
        }
        host.appendChild(el);
        var glitchTimer = setInterval(function () {
            var n = Math.max(2, Math.round(spans.length * Math.pow(ART_INV_PHI, 3)));
            for (var i = 0; i < n; i++) spans[Math.floor(Math.random() * spans.length)].textContent = artGlyph();
        }, Math.round(76 * ART_PHI));
        setTimeout(function () {
            clearInterval(glitchTimer);
            if (!el.__predated && el.parentNode) el.parentNode.removeChild(el);
        }, 7400);
    }

    // ── Variant: CENTIPEDE — multi-segment chain, peristaltic wave + head feelers ──
    // Multiple overlapping Fibonacci grids form one undulating body. Each segment sways
    // perpendicular to travel with phase offset; the leader emits feelers. Whole chain crawls.
    function spawnArtifactCentipede(host) {
        var el = artNewContainer();
        var clusterAngle = Math.random() * Math.PI * 2;
        var cosA = Math.cos(clusterAngle), sinA = Math.sin(clusterAngle);
        var perpX = -sinA, perpY = cosA;
        var crawlDist = 144 + Math.random() * 89;
        el.style.setProperty('--cdx', Math.round(cosA * crawlDist) + 'px');
        el.style.setProperty('--cdy', Math.round(sinA * crawlDist) + 'px');
        el.style.animation = 'cyberspace-artifact-crawl 7.2s ease-in-out forwards';
        var segCount = pick([3, 5, 8]);
        var segStep  = 21 + Math.random() * 13;
        var allSpans = [], leadingPool = [];
        for (var seg = 0; seg < segCount; seg++) {
            var segEl = document.createElement('div');
            segEl.className = 'cyberspace-artifact-seg';
            var segOffsetDist = -seg * segStep;
            var segLeft = Math.round(cosA * segOffsetDist);
            var segTop  = Math.round(sinA * segOffsetDist);
            segEl.style.left = segLeft + 'px';
            segEl.style.top  = segTop  + 'px';
            var swMag = 5 + Math.random() * 5;
            segEl.style.setProperty('--swx', Math.round(perpX * swMag) + 'px');
            segEl.style.setProperty('--swy', Math.round(perpY * swMag) + 'px');
            var swDur = (1.6 + Math.random() * 1.0).toFixed(2);
            var swPhase = (-seg * parseFloat(swDur) / segCount * ART_PHI).toFixed(2);
            segEl.style.animation = 'cyberspace-art-seg-wave ' + swDur + 's ease-in-out ' + swPhase + 's infinite';
            var cols = pick([3, 5]);
            var rows = pick([3, 5]);
            var g = artBuildFibGrid(cols, rows);
            for (var row = 0; row < rows; row++) {
                for (var col = 0; col < cols; col++) {
                    var span = document.createElement('span');
                    span.className = 'cyberspace-artifact-char';
                    var leftPx = Math.round(g.colX[col] - g.totalW / 2);
                    var topPx  = Math.round(g.rowY[row] - g.totalH / 2);
                    span.style.left = leftPx + 'px';
                    span.style.top  = topPx + 'px';
                    var cellMin = Math.min(g.colW[col], g.rowH[row]);
                    var fontPx  = Math.max(8, Math.round(cellMin * ART_PHI * ART_INV_PHI));
                    span.style.fontSize = fontPx + 'px';
                    var uAng = Math.random() * Math.PI * 2;
                    var uMag = 3 + Math.random() * 4;
                    span.style.setProperty('--ux', Math.round(Math.cos(uAng) * uMag) + 'px');
                    span.style.setProperty('--uy', Math.round(Math.sin(uAng) * uMag) + 'px');
                    var undDur   = (2.0 + Math.random() * 1.6).toFixed(2);
                    var undDelay = (-Math.random() * 3).toFixed(2);
                    span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite, '
                                         + 'cyberspace-art-undulate ' + undDur + 's ease-in-out ' + undDelay + 's infinite';
                    span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                    segEl.appendChild(span);
                    allSpans.push(span);
                    if (seg === 0) {
                        var ccx = leftPx + g.colW[col] / 2;
                        var ccy = topPx  + g.rowH[row] / 2;
                        leadingPool.push({ left: leftPx + segLeft, top: topPx + segTop, fontPx: fontPx, score: ccx * cosA + ccy * sinA });
                    }
                }
            }
            el.appendChild(segEl);
        }
        leadingPool.sort(function (a, b) { return b.score - a.score; });
        leadingPool = leadingPool.slice(0, Math.max(1, Math.round(leadingPool.length * (1 - ART_INV_PHI))));
        host.appendChild(el);
        var glitchTimer = setInterval(function () {
            var n = Math.max(2, Math.round(allSpans.length * Math.pow(ART_INV_PHI, 3)));
            for (var i = 0; i < n; i++) allSpans[Math.floor(Math.random() * allSpans.length)].textContent = artGlyph();
        }, Math.round(76 * ART_PHI));
        var feelerTimer = setInterval(function () {
            if (!leadingPool.length) return;
            var seed = leadingPool[Math.floor(Math.random() * leadingPool.length)];
            var feel = document.createElement('span');
            feel.className = 'cyberspace-artifact-char';
            feel.style.left = seed.left + 'px';
            feel.style.top  = seed.top  + 'px';
            feel.style.fontSize = seed.fontPx + 'px';
            feel.style.setProperty('--abl', (0.4 + Math.random() * ART_INV_PHI).toFixed(2) + 'px');
            var fdist  = 55 + Math.random() * 89;
            var spread = (Math.random() - 0.5) * 0.42;
            feel.style.setProperty('--adx1', Math.round(Math.cos(clusterAngle + spread) * fdist) + 'px');
            feel.style.setProperty('--ady1', Math.round(Math.sin(clusterAngle + spread) * fdist) + 'px');
            var fdur = ART_PHI + Math.random() * ART_PHI;
            feel.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite, cyberspace-art-drift ' + fdur.toFixed(2) + 's ease-in 0s forwards';
            feel.textContent = artGlyph();
            el.appendChild(feel);
            setTimeout(function () { if (feel.parentNode) feel.parentNode.removeChild(feel); }, fdur * 1000 + 250);
        }, 89);
        setTimeout(function () {
            clearInterval(glitchTimer); clearInterval(feelerTimer);
            if (!el.__predated && el.parentNode) el.parentNode.removeChild(el);
        }, 7400);
    }

    // ── Variant: PULSE — concentric Fibonacci rings beating in lub-dub rhythm ──────
    // Cells live on Fibonacci-count rings at increasing radii. A heartbeat animation pushes
    // each cell radially outward in a quick double-pulse, then rests. The whole organism
    // drifts slowly across the screen (alive in fluid). Each spawn rolls a SIZE class —
    // current dimensions are the LARGEST; smaller variants exist (distant / younger).
    // ~25% of PULSE spawns "corrupt" partway through life: rings scatter outward, each cell
    // becoming a wasp glyph in red and fading out — the organism falling apart.
    function spawnArtifactPulse(host) {
        var el = artNewContainer();
        // Drift — gentler than SLUG. The organism floats while it beats.
        var clusterAngle = Math.random() * Math.PI * 2;
        var crawlDist = 65 + Math.random() * 55;
        el.style.setProperty('--cdx', Math.round(Math.cos(clusterAngle) * crawlDist) + 'px');
        el.style.setProperty('--cdy', Math.round(Math.sin(clusterAngle) * crawlDist) + 'px');
        el.style.animation = 'cyberspace-artifact-crawl 8.6s ease-in-out forwards';

        // Size tier — 1.0 is the largest (matches the original feel). Smaller tiers are rarer.
        var scale = pick([0.42, 0.42, 0.62, 0.62, 0.82, 1.0, 1.0]);
        var rings = [
            { count: 1,  radius: 0      * scale, ampl: 0       },
            { count: 5,  radius: 18     * scale, ampl: 4  * scale },
            { count: 8,  radius: 36     * scale, ampl: 7  * scale },
            { count: 13, radius: 58     * scale, ampl: 11 * scale },
            { count: 21, radius: 84     * scale, ampl: 15 * scale }
        ];
        var spans = [];
        rings.forEach(function (ring, ringIdx) {
            for (var i = 0; i < ring.count; i++) {
                var ang = (i / ring.count) * Math.PI * 2 + ringIdx * 0.13;
                var cx = Math.cos(ang) * ring.radius;
                var cy = Math.sin(ang) * ring.radius;
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = Math.round(cx) + 'px';
                span.style.top  = Math.round(cy) + 'px';
                // Font scales with size tier but never below readable
                span.style.fontSize = Math.max(8, Math.round((10 + Math.random() * 4) * (0.6 + scale * 0.4))) + 'px';
                span.style.setProperty('--px', (Math.cos(ang) * ring.ampl).toFixed(2) + 'px');
                span.style.setProperty('--py', (Math.sin(ang) * ring.ampl).toFixed(2) + 'px');
                var beatDelay = ringIdx * 0.08;
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite, '
                                     + 'cyberspace-art-heartbeat 1.6s ease-in-out ' + beatDelay + 's infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                spans.push({ el: span, ang: ang, radius: ring.radius });
            }
        });
        host.appendChild(el);
        var glitchTimer = setInterval(function () {
            var n = Math.max(2, Math.round(spans.length * Math.pow(ART_INV_PHI, 3)));
            for (var i = 0; i < n; i++) spans[Math.floor(Math.random() * spans.length)].el.textContent = artGlyph();
        }, 130);

        // CORRUPTION — ~25% of PULSE spawns fall apart. At a random point 55–75% through life,
        // every cell scatters radially outward, swaps to a hostile glyph in red, and fades.
        var willCorrupt = Math.random() < 0.25;
        var totalLife  = 8800;
        if (willCorrupt) {
            var corruptAt = Math.round(totalLife * (0.55 + Math.random() * 0.20));
            setTimeout(function () {
                var WASP = '#@!*&%§¶†‡¦¤¥×÷±¬¿¡';
                spans.forEach(function (s) {
                    var scatterAng  = s.ang + (Math.random() - 0.5) * 0.7;
                    var scatterDist = 30 + Math.random() * 90;
                    var tx = Math.round(Math.cos(scatterAng) * scatterDist);
                    var ty = Math.round(Math.sin(scatterAng) * scatterDist);
                    // Drop the heartbeat — only the flicker continues until it's gone
                    s.el.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                    s.el.style.transition = 'transform 0.9s cubic-bezier(0.2, 0.6, 0.4, 1), opacity 0.9s ease, color 0.2s ease';
                    s.el.style.transform = 'translate(' + tx + 'px, ' + ty + 'px)';
                    s.el.style.color = '#ff2244';
                    s.el.style.opacity = '0';
                    s.el.textContent = WASP[Math.floor(Math.random() * WASP.length)];
                });
                setTimeout(function () { clearInterval(glitchTimer); }, 950);
            }, corruptAt);
        }

        setTimeout(function () {
            clearInterval(glitchTimer);
            if (!el.__predated && el.parentNode) el.parentNode.removeChild(el);
        }, totalLife);
    }

    // ── Variant: WANDERER — small cluster that walks the screen, pausing to "look around" ──
    // JS-driven motion (not CSS animation) so it can change direction mid-flight. Picks a random
    // target, walks toward it, arrives, pauses with a gentle wobble + breathing scale, picks a
    // new target. Self-aware feel: it stops, surveys, then continues.
    function spawnArtifactWanderer(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var cols = 5, rows = 3;
        var g = artBuildFibGrid(cols, rows);
        var spans = [];
        for (var row = 0; row < rows; row++) {
            for (var col = 0; col < cols; col++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = Math.round(g.colX[col] - g.totalW / 2) + 'px';
                span.style.top  = Math.round(g.rowY[row] - g.totalH / 2) + 'px';
                var cellMin = Math.min(g.colW[col], g.rowH[row]);
                span.style.fontSize = Math.max(8, Math.round(cellMin * ART_INV_PHI)) + 'px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                spans.push(span);
            }
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });

        var posX = 0, posY = 0, targetX = 0, targetY = 0;
        var pauseUntil = 0;
        var startTs = performance.now();
        var ttl = 9500;
        var lastTs = null;
        var killed = false;

        function pickNewTarget() {
            var ang = Math.random() * Math.PI * 2;
            var dist = 35 + Math.random() * 70;
            targetX = posX + Math.cos(ang) * dist;
            targetY = posY + Math.sin(ang) * dist;
            // Soft bounds — keep the wanderer roughly local to its spawn (within ±150px)
            targetX = Math.max(-180, Math.min(180, targetX));
            targetY = Math.max(-150, Math.min(150, targetY));
        }
        pickNewTarget();

        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            if (!lastTs) lastTs = ts;
            var dt = Math.min(0.05, (ts - lastTs) / 1000);
            lastTs = ts;
            var sc = 1;
            if (ts < pauseUntil) {
                // Looking around — small head-wobble + breathing scale (sentient pause)
                var wobble = Math.sin((ts - startTs) / 70) * 1.8;
                sc = 1 + Math.sin((ts - startTs) / 220) * 0.08;
                el.style.transform = 'translate(' + (posX + wobble) + 'px, ' + posY + 'px) scale(' + sc.toFixed(3) + ')';
                requestAnimationFrame(step);
                return;
            }
            var dx = targetX - posX, dy = targetY - posY;
            var dist = Math.hypot(dx, dy);
            if (dist < 5) {
                // Arrived — pause to look around, then pick the next destination
                pauseUntil = ts + 500 + Math.random() * 900;
                setTimeout(function () { if (!killed) pickNewTarget(); }, 200);
            } else {
                var speed = 28; // px/s
                var adv = Math.min(dist, speed * dt);
                posX += (dx / dist) * adv;
                posY += (dy / dist) * adv;
                el.style.transform = 'translate(' + posX.toFixed(1) + 'px, ' + posY.toFixed(1) + 'px) scale(1)';
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);

        var glitchTimer = setInterval(function () {
            var n = Math.max(2, Math.round(spans.length * 0.20));
            for (var i = 0; i < n; i++) spans[Math.floor(Math.random() * spans.length)].textContent = artGlyph();
        }, 140);
        setTimeout(function () {
            killed = true;
            clearInterval(glitchTimer);
            el.style.transition = 'opacity 0.4s ease';
            el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }

    // ── Variant: SPIDER — 8-leg radial cluster, alternate-pulse walking gait ───────
    // Central hub glyph anchors the body. 8 legs spread radially, each a 3-glyph chain.
    // Legs are paired (i, i+4) and each pair pulses (scale + opacity) on a phase offset
    // so the spider reads as walking. Hub drifts slowly.
    function spawnArtifactSpider(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var LEGS = 8, SEG_PER_LEG = 3, segLen = 12;
        var legSpans = [];
        for (var leg = 0; leg < LEGS; leg++) {
            var legAngle = (leg / LEGS) * Math.PI * 2;
            var legCx = Math.cos(legAngle), legCy = Math.sin(legAngle);
            var segs = [];
            for (var s = 0; s < SEG_PER_LEG; s++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                var r = (s + 1) * segLen;
                span.style.left = Math.round(legCx * r) + 'px';
                span.style.top  = Math.round(legCy * r) + 'px';
                span.style.fontSize = Math.max(8, Math.round(13 - s * 1.5)) + 'px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                segs.push(span);
            }
            legSpans.push(segs);
        }
        var hub = document.createElement('span');
        hub.className = 'cyberspace-artifact-char';
        hub.style.left = '0px'; hub.style.top = '0px';
        hub.style.fontSize = '16px';
        hub.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
        hub.textContent = artGlyph();
        el.appendChild(hub);
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now();
        var ttl = 8200, killed = false;
        var driftAng = Math.random() * Math.PI * 2;
        var driftMag = 0.012; // px per ms — very slow body drift
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            // Walking-gait: each leg pair (i, i+4) pulses opposite-phase. Eight legs → four phases.
            for (var leg = 0; leg < LEGS; leg++) {
                var phaseLeg = (leg % 4) / 4;
                var phase = (elapsed / 420 + phaseLeg) * Math.PI * 2;
                var pulse = (Math.sin(phase) * 0.5 + 0.5);
                for (var s = 0; s < SEG_PER_LEG; s++) {
                    var sp = legSpans[leg][s];
                    var opacity = (0.45 + pulse * 0.55).toFixed(2);
                    var scale = (0.85 + pulse * 0.30).toFixed(2);
                    sp.style.opacity = opacity;
                    sp.style.transform = 'scale(' + scale + ')';
                }
            }
            var dx = Math.cos(driftAng) * driftMag * elapsed;
            var dy = Math.sin(driftAng) * driftMag * elapsed;
            el.style.transform = 'translate(' + dx.toFixed(1) + 'px,' + dy.toFixed(1) + 'px)';
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            hub.textContent = artGlyph();
            for (var i = 0; i < 2; i++) {
                var pickLeg = Math.floor(Math.random() * LEGS);
                var pickSeg = Math.floor(Math.random() * SEG_PER_LEG);
                legSpans[pickLeg][pickSeg].textContent = artGlyph();
            }
        }, 160);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }

    // ── Variant: INCHWORM — body bunches and extends, translates only on the extend phase ──
    // Linear chain of glyphs along an angle vector. Body cycles: BUNCH (all crowd near head) →
    // EXTEND (spread out + head pulls forward) → BUNCH again. Direction-anchored, head-led.
    function spawnArtifactInchworm(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var N = 7;
        var ang = Math.random() * Math.PI * 2;
        var ca = Math.cos(ang), sa = Math.sin(ang);
        var maxSpread = 56, stepDist = 22;
        var spans = [];
        for (var i = 0; i < N; i++) {
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            span.style.fontSize = Math.max(8, Math.round(14 - i * 0.7)) + 'px';
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
            spans.push(span);
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8400, killed = false;
        var headX = 0, headY = 0;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            var cycle = 1400; // ms per bunch-extend
            var phase = (elapsed % cycle) / cycle; // 0..1
            var extend = 0.5 - 0.5 * Math.cos(phase * Math.PI * 2); // 0 → 1 → 0
            // On the leading edge of extend (phase 0→0.5), the head pulls forward;
            // the tail catches up on the trailing edge (0.5→1).
            if (phase < 0.5) {
                headX += ca * stepDist * 0.014;
                headY += sa * stepDist * 0.014;
            }
            for (var i = 0; i < N; i++) {
                var t = i / (N - 1); // 0 at head, 1 at tail
                var spread = t * maxSpread * extend;
                var x = headX - ca * spread;
                var y = headY - sa * spread;
                spans[i].style.left = Math.round(x) + 'px';
                spans[i].style.top  = Math.round(y) + 'px';
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            spans[Math.floor(Math.random() * N)].textContent = artGlyph();
        }, 130);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }


    // ── Variant: HOPPER — crouch + parabolic launch + land cycle ──────────────────
    // Compact glyph cluster. Phase 0: idle. Phase 1: crouch (vertical squash). Phase 2:
    // launch in a parabolic arc along an angle. Phase 3: land + return scale. Loops.
    function spawnArtifactHopper(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var cluster = document.createElement('div');
        cluster.style.position = 'absolute';
        cluster.style.left = '0px'; cluster.style.top = '0px';
        cluster.style.transformOrigin = '50% 100%';
        el.appendChild(cluster);
        var cols = 3, rows = 2;
        var g = artBuildFibGrid(cols, rows);
        var spans = [];
        for (var row = 0; row < rows; row++) {
            for (var col = 0; col < cols; col++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = Math.round(g.colX[col] - g.totalW / 2) + 'px';
                span.style.top  = Math.round(g.rowY[row] - g.totalH / 2) + 'px';
                var cellMin = Math.min(g.colW[col], g.rowH[row]);
                span.style.fontSize = Math.max(8, Math.round(cellMin * ART_INV_PHI)) + 'px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                cluster.appendChild(span);
                spans.push(span);
            }
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8400, killed = false;
        var hopAng = Math.random() * Math.PI * 2;
        var hopDist = 35 + Math.random() * 30;
        var posX = 0, posY = 0;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            var cycle = 1600;
            var p = (elapsed % cycle) / cycle; // 0..1
            var scaleY = 1, scaleX = 1, arcDx = 0, arcDy = 0;
            if (p < 0.20) {
                // crouch: scaleY < 1, scaleX > 1
                var k = p / 0.20;
                scaleY = 1 - 0.30 * k; scaleX = 1 + 0.18 * k;
            } else if (p < 0.65) {
                // launch arc
                var k = (p - 0.20) / 0.45;
                scaleY = 1 + 0.25 * Math.sin(k * Math.PI); scaleX = 1 - 0.12 * Math.sin(k * Math.PI);
                arcDx = Math.cos(hopAng) * hopDist * k;
                // parabola: max at k=0.5
                arcDy = Math.sin(hopAng) * hopDist * k - Math.sin(k * Math.PI) * 18;
            } else if (p < 0.80) {
                // land squash
                var k = (p - 0.65) / 0.15;
                scaleY = 1 - 0.22 * Math.sin(k * Math.PI); scaleX = 1 + 0.14 * Math.sin(k * Math.PI);
                arcDx = Math.cos(hopAng) * hopDist;
                arcDy = Math.sin(hopAng) * hopDist;
                if (k > 0.95) { posX += arcDx; posY += arcDy; }
            } else {
                // idle wobble before next hop
                arcDx = Math.cos(hopAng) * hopDist;
                arcDy = Math.sin(hopAng) * hopDist;
                if (p > 0.81 && p < 0.83) { posX += arcDx; posY += arcDy; hopAng = Math.random() * Math.PI * 2; hopDist = 35 + Math.random() * 30; arcDx = 0; arcDy = 0; }
            }
            el.style.transform = 'translate(' + (posX + arcDx).toFixed(1) + 'px,' + (posY + arcDy).toFixed(1) + 'px)';
            cluster.style.transform = 'scale(' + scaleX.toFixed(3) + ',' + scaleY.toFixed(3) + ')';
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            spans[Math.floor(Math.random() * spans.length)].textContent = artGlyph();
        }, 150);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }


    // ── Variant: JELLY — concentric rings pulse outward/in; vertical drift ───────
    // Concentric Fibonacci rings of glyphs at growing radii. Each ring's effective radius
    // oscillates around a base via a phase-delayed sine, producing an outward-traveling pulse.
    // Whole body drifts vertically (up or down, randomised) and slightly sideways.
    function spawnArtifactJelly(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var RINGS = [5, 8, 13];
        var ringSpans = [];
        for (var ri = 0; ri < RINGS.length; ri++) {
            var n = RINGS[ri];
            var baseR = 10 + ri * 14;
            var ring = [];
            for (var i = 0; i < n; i++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.fontSize = Math.max(8, Math.round(13 - ri * 1.5)) + 'px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                ring.push({ el: span, angle: (i / n) * Math.PI * 2, baseR: baseR, ringIdx: ri });
            }
            ringSpans.push(ring);
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 9000, killed = false;
        var driftAng = Math.random() * Math.PI * 0.5 - Math.PI * 0.25 + (Math.random() < 0.5 ? Math.PI / 2 : -Math.PI / 2);
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            // Outward-traveling pulse: each ring's expansion phase is delayed by its index
            for (var ri = 0; ri < ringSpans.length; ri++) {
                var ring = ringSpans[ri];
                var phase = (elapsed / 1100) * Math.PI * 2 - ri * 0.9;
                var pulse = Math.sin(phase);
                var rEff = ring[0].baseR + pulse * 6;
                for (var i = 0; i < ring.length; i++) {
                    var p = ring[i];
                    var x = Math.cos(p.angle) * rEff;
                    var y = Math.sin(p.angle) * rEff * 0.78; // jelly-bell aspect
                    p.el.style.left = Math.round(x) + 'px';
                    p.el.style.top  = Math.round(y) + 'px';
                    p.el.style.opacity = (0.45 + 0.45 * (Math.sin(phase + i * 0.4) * 0.5 + 0.5)).toFixed(2);
                }
            }
            var drift = elapsed * 0.020;
            el.style.transform = 'translate(' + (Math.cos(driftAng) * drift).toFixed(1) + 'px,' + (Math.sin(driftAng) * drift).toFixed(1) + 'px)';
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var allSpans = [];
        for (var ri = 0; ri < ringSpans.length; ri++) for (var i = 0; i < ringSpans[ri].length; i++) allSpans.push(ringSpans[ri][i].el);
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 3; i++) allSpans[Math.floor(Math.random() * allSpans.length)].textContent = artGlyph();
        }, 150);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }

    // ── Variant: SQUID — pulse-jet propulsion with trailing tentacles ────────────
    // Bell-shaped body up front. 4 tentacle chains trail behind. Body squashes vertically
    // (jet burst) then relaxes (glide). Translation accumulates on burst phases. Tentacles
    // wave on glide phases (less violent on burst).
    function spawnArtifactSquid(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var ang = Math.random() * Math.PI * 2;
        var ca = Math.cos(ang), sa = Math.sin(ang);
        var perpX = -sa, perpY = ca;
        // Bell body — 3x2 cluster up front
        var bell = document.createElement('div');
        bell.style.position = 'absolute'; bell.style.left = '0px'; bell.style.top = '0px';
        bell.style.transformOrigin = '50% 100%';
        el.appendChild(bell);
        var bellSpans = [];
        for (var row = 0; row < 2; row++) {
            for (var col = 0; col < 3; col++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = (col * 8 - 8) + 'px';
                span.style.top  = (row * 7 - 14) + 'px';
                span.style.fontSize = '13px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                bell.appendChild(span);
                bellSpans.push(span);
            }
        }
        // 4 tentacles trailing OPPOSITE to forward direction
        var tentSpans = [];
        for (var t = 0; t < 4; t++) {
            var lateral = (t - 1.5) * 5;
            var chain = [];
            for (var s = 0; s < 5; s++) {
                var span2 = document.createElement('span');
                span2.className = 'cyberspace-artifact-char';
                span2.style.fontSize = '10px';
                span2.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span2.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span2);
                chain.push({ el: span2, lateralOffset: lateral, seg: s });
            }
            tentSpans.push(chain);
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8800, killed = false;
        var posAlong = 0;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            var cycle = 1300;
            var p = (elapsed % cycle) / cycle;
            // Burst (0-0.25): body squashes, translation kicks. Glide (0.25-1): relax.
            var bellSY = 1, bellSX = 1, jet = 0;
            if (p < 0.25) {
                var k = p / 0.25;
                bellSY = 1 - 0.40 * Math.sin(k * Math.PI); bellSX = 1 + 0.22 * Math.sin(k * Math.PI);
                jet = k;
            } else {
                bellSY = 1 + 0.05 * Math.sin((p - 0.25) * Math.PI * 4); bellSX = 1;
            }
            posAlong += jet * 1.4;
            var advance = ca * posAlong, advanceY = sa * posAlong;
            el.style.transform = 'translate(' + advance.toFixed(1) + 'px,' + advanceY.toFixed(1) + 'px)';
            bell.style.transform = 'scale(' + bellSX.toFixed(3) + ',' + bellSY.toFixed(3) + ')';
            // Tentacles trail backward (opposite to forward direction). Wave during glide.
            for (var ti = 0; ti < tentSpans.length; ti++) {
                var chain = tentSpans[ti];
                for (var s = 0; s < chain.length; s++) {
                    var node = chain[s];
                    var backDist = -10 - s * 7; // negative = behind
                    var waveAmp = p > 0.25 ? 4 + s * 0.8 : 1.5;
                    var wave = Math.sin(elapsed / 230 + s * 0.6) * waveAmp;
                    var x = ca * backDist + perpX * (node.lateralOffset + wave);
                    var y = sa * backDist + perpY * (node.lateralOffset + wave);
                    node.el.style.left = Math.round(x) + 'px';
                    node.el.style.top  = Math.round(y) + 'px';
                }
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            bellSpans[Math.floor(Math.random() * bellSpans.length)].textContent = artGlyph();
            var ch = tentSpans[Math.floor(Math.random() * tentSpans.length)];
            ch[Math.floor(Math.random() * ch.length)].el.textContent = artGlyph();
        }, 130);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }

    // ── Variant: BEETLE — scurry-stop-scurry rhythm with shell shimmer + antennae ─
    // Compact rectangular body. Bursts of fast motion alternated with full stops. The shell
    // glyphs shimmer (rapid glyph swap) distinctly faster than the standard idle glitch.
    // Two single-glyph antennae sit at the front and vibrate.
    function spawnArtifactBeetle(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var ang = Math.random() * Math.PI * 2;
        var ca = Math.cos(ang), sa = Math.sin(ang);
        var perpX = -sa, perpY = ca;
        // Shell — 4x3 grid
        var shell = [];
        for (var row = 0; row < 3; row++) {
            for (var col = 0; col < 4; col++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                var lx = (col - 1.5) * 7;
                var ly = (row - 1) * 7;
                // Rotate to body axis
                span.style.left = Math.round(ca * lx - sa * ly) + 'px';
                span.style.top  = Math.round(sa * lx + ca * ly) + 'px';
                span.style.fontSize = '12px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                shell.push(span);
            }
        }
        // Two antennae at front (along ca,sa direction), splayed perpendicular
        var antL = document.createElement('span');
        antL.className = 'cyberspace-artifact-char'; antL.style.fontSize = '10px';
        antL.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
        antL.textContent = artGlyph();
        var antR = antL.cloneNode(true);
        el.appendChild(antL); el.appendChild(antR);
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8400, killed = false;
        var posDist = 0, scurryPhase = 0;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            // Cycle: SCURRY (fast, 0-0.35) then STOP (still, 0.35-1.0)
            var cycle = 1700;
            var p = (elapsed % cycle) / cycle;
            var moving = p < 0.35;
            if (moving) posDist += 1.7;
            var x = ca * posDist, y = sa * posDist;
            el.style.transform = 'translate(' + x.toFixed(1) + 'px,' + y.toFixed(1) + 'px)';
            // Antennae positions: at front (along axis), splayed
            var frontX = ca * 13, frontY = sa * 13;
            var splayBase = 5;
            var vibrate = Math.sin(elapsed / 60) * 1.4; // fast jitter
            antL.style.left = Math.round(frontX + perpX * (splayBase + vibrate)) + 'px';
            antL.style.top  = Math.round(frontY + perpY * (splayBase + vibrate)) + 'px';
            antR.style.left = Math.round(frontX - perpX * (splayBase - vibrate)) + 'px';
            antR.style.top  = Math.round(frontY - perpY * (splayBase - vibrate)) + 'px';
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var shimmer = setInterval(function () {
            // Shell shimmer — many glyphs swap quickly when moving, few when stopped
            var n = 5;
            for (var i = 0; i < n; i++) shell[Math.floor(Math.random() * shell.length)].textContent = artGlyph();
        }, 90);
        var antTimer = setInterval(function () { antL.textContent = artGlyph(); antR.textContent = artGlyph(); }, 80);
        setTimeout(function () {
            killed = true; clearInterval(shimmer); clearInterval(antTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }

    // ── Variant: TADPOLE — small body + long whipping sinusoidal tail ────────────
    // Tight head cluster + a chain of glyphs forming a tail. Tail oscillates laterally with
    // phase delay (S-wave). Body advances in pulses synchronized with tail strokes (zero-crossing
    // of the head-end of the tail wave → small forward kick).
    function spawnArtifactTadpole(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var ang = Math.random() * Math.PI * 2;
        var ca = Math.cos(ang), sa = Math.sin(ang);
        var perpX = -sa, perpY = ca;
        // Head — 3 glyphs in a small triangle around origin
        var headSpans = [];
        var headOffsets = [[0, 0], [4, -3], [4, 3]];
        for (var i = 0; i < headOffsets.length; i++) {
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            var hx = headOffsets[i][0], hy = headOffsets[i][1];
            span.style.left = Math.round(ca * hx - sa * hy) + 'px';
            span.style.top  = Math.round(sa * hx + ca * hy) + 'px';
            span.style.fontSize = '13px';
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
            headSpans.push(span);
        }
        // Tail — 9 glyphs trailing behind (opposite of body axis)
        var TAIL_N = 9;
        var tailSpans = [];
        for (var i = 0; i < TAIL_N; i++) {
            var span2 = document.createElement('span');
            span2.className = 'cyberspace-artifact-char';
            span2.style.fontSize = Math.max(8, Math.round(12 - i * 0.4)) + 'px';
            span2.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span2.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span2);
            tailSpans.push(span2);
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8800, killed = false;
        var posDist = 0;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            // Tail wave — frequency tuned for "whipping" feel; head propels on stroke peaks
            var omega = elapsed / 170;
            var headWave = Math.sin(omega);
            posDist += Math.max(0, Math.abs(headWave) - 0.5) * 0.9; // forward kick on extrema
            for (var i = 0; i < TAIL_N; i++) {
                var s = -8 - i * 6; // distance behind head along body axis
                var lateral = Math.sin(omega - i * 0.6) * (4 + i * 0.8);
                var x = ca * s + perpX * lateral;
                var y = sa * s + perpY * lateral;
                tailSpans[i].style.left = Math.round(x) + 'px';
                tailSpans[i].style.top  = Math.round(y) + 'px';
            }
            el.style.transform = 'translate(' + (ca * posDist).toFixed(1) + 'px,' + (sa * posDist).toFixed(1) + 'px)';
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            headSpans[Math.floor(Math.random() * headSpans.length)].textContent = artGlyph();
            tailSpans[Math.floor(Math.random() * tailSpans.length)].textContent = artGlyph();
        }, 130);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }

    // ── Variant: ANT — bezier wanderer; long curving legs, stop, reorient, repeat ─
    // Each WALK leg is a quadratic bezier from current pos to a new target. The first
    // control point sits along the current heading (G1 continuity — the curve starts
    // tangent to the previous leg, no snap), and the second control point is offset
    // perpendicular to give the curve a real sweep. The body's heading at any moment is
    // the bezier tangent, so the ant glyphs face the way they're actually moving.
    function spawnArtifactAnt(host) {
        var el = artNewContainer();
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';
        var body = [];
        for (var i = 0; i < 3; i++) {
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            span.style.fontSize = (i === 1 ? 12 : 11) + 'px';
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
            body.push(span);
        }
        var antL = document.createElement('span'), antR = document.createElement('span');
        antL.className = antR.className = 'cyberspace-artifact-char';
        antL.style.fontSize = antR.style.fontSize = '9px';
        antL.style.animation = antR.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
        antL.textContent = artGlyph(); antR.textContent = artGlyph();
        el.appendChild(antL); el.appendChild(antR);
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 9000, killed = false;
        var posX = 0, posY = 0;
        var heading = Math.random() * Math.PI * 2;
        var state = 'WALK';
        var stateEndAt = 0;
        var leg = null;     // active bezier: { p0, p1, p2, startTs, duration }
        var SPEED = 55;     // px/sec average — drives target distance
        function pickHeadingOffset() {
            // Small turns common (~70% < ±50°), occasional sharp turns (~30%, up to ±135°)
            var sign = Math.random() < 0.5 ? -1 : 1;
            var mag = Math.random() < 0.70 ? Math.random() * 0.9
                                            : 1.0 + Math.random() * 1.4;
            heading += sign * mag;
        }
        function startNewLeg(ts) {
            // If we're near the edge, point inward before laying down the new leg
            if (Math.abs(posX) > 140 || Math.abs(posY) > 110) {
                heading = Math.atan2(-posY, -posX) + (Math.random() - 0.5) * 0.6;
            }
            var ca = Math.cos(heading), sa = Math.sin(heading);
            var perpX = -sa, perpY = ca;
            var duration = 1500 + Math.random() * 2000;            // 1.5-3.5s — longer arcs
            var targetDist = SPEED * (duration / 1000);            // ~80-190 px per leg
            var lateralBias = (Math.random() - 0.5) * targetDist * 0.55;  // ±27.5% sideways
            var tangentDist = targetDist * 0.40;                   // P1 along heading from P0
            leg = {
                p0: [posX, posY],
                p1: [posX + ca * tangentDist, posY + sa * tangentDist],
                p2: [posX + ca * targetDist + perpX * lateralBias,
                     posY + sa * targetDist + perpY * lateralBias],
                startTs: ts, duration: duration
            };
            stateEndAt = ts + duration;
        }
        function bezAt(t) {
            var u = 1 - t;
            return [ u*u*leg.p0[0] + 2*u*t*leg.p1[0] + t*t*leg.p2[0],
                     u*u*leg.p0[1] + 2*u*t*leg.p1[1] + t*t*leg.p2[1] ];
        }
        function bezTan(t) {
            var u = 1 - t;
            return [ 2*u*(leg.p1[0]-leg.p0[0]) + 2*t*(leg.p2[0]-leg.p1[0]),
                     2*u*(leg.p1[1]-leg.p0[1]) + 2*t*(leg.p2[1]-leg.p1[1]) ];
        }
        startNewLeg(performance.now());
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            if (state === 'WALK') {
                var t = (ts - leg.startTs) / leg.duration;
                if (t >= 1) {
                    posX = leg.p2[0]; posY = leg.p2[1];
                    var tanEnd = bezTan(1);
                    heading = Math.atan2(tanEnd[1], tanEnd[0]);
                    state = 'PAUSE';
                    stateEndAt = ts + 300 + Math.random() * 500;
                } else {
                    var p = bezAt(t);
                    posX = p[0]; posY = p[1];
                    var tan = bezTan(t);
                    heading = Math.atan2(tan[1], tan[0]);
                }
            } else { // PAUSE — antennae keep twitching while body holds
                if (ts > stateEndAt) {
                    pickHeadingOffset();
                    state = 'WALK';
                    startNewLeg(ts);
                }
            }
            var ca = Math.cos(heading), sa = Math.sin(heading);
            for (var i = 0; i < body.length; i++) {
                var s = (i - 1) * 5;
                body[i].style.left = Math.round(posX + ca * s) + 'px';
                body[i].style.top  = Math.round(posY + sa * s) + 'px';
            }
            var perpX = -sa, perpY = ca;
            var frontX = posX + ca * 9, frontY = posY + sa * 9;
            var vibL = Math.sin(ts / 55) * 1.8;
            var vibR = Math.sin(ts / 55 + Math.PI) * 1.8;
            antL.style.left = Math.round(frontX + perpX * (4 + vibL)) + 'px';
            antL.style.top  = Math.round(frontY + perpY * (4 + vibL)) + 'px';
            antR.style.left = Math.round(frontX - perpX * (4 - vibR)) + 'px';
            antR.style.top  = Math.round(frontY - perpY * (4 - vibR)) + 'px';
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            body[Math.floor(Math.random() * body.length)].textContent = artGlyph();
        }, 140);
        var antTimer = setInterval(function () { antL.textContent = artGlyph(); antR.textContent = artGlyph(); }, 90);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer); clearInterval(antTimer);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }


    // ── Variant: MOIRE — two overlapping rotated grids producing interference ────
    function spawnArtifactMoire(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var COLS = 7, ROWS = 7, SP = 9;
        function buildGrid(angle, alpha) {
            var grid = document.createElement('div');
            grid.style.position = 'absolute'; grid.style.left = '0px'; grid.style.top = '0px';
            grid.style.transformOrigin = '50% 50%';
            grid.style.opacity = alpha;
            var ca = Math.cos(angle), sa = Math.sin(angle);
            var spans = [];
            for (var r = 0; r < ROWS; r++) {
                for (var c = 0; c < COLS; c++) {
                    var lx = (c - (COLS - 1) / 2) * SP;
                    var ly = (r - (ROWS - 1) / 2) * SP;
                    var span = document.createElement('span');
                    span.className = 'cyberspace-artifact-char';
                    span.style.left = Math.round(ca * lx - sa * ly) + 'px';
                    span.style.top  = Math.round(sa * lx + ca * ly) + 'px';
                    span.style.fontSize = '10px';
                    span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                    span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                    grid.appendChild(span);
                    spans.push(span);
                }
            }
            return { el: grid, spans: spans };
        }
        var g1 = buildGrid(0, '0.55');
        var g2 = buildGrid(0.12, '0.45');
        el.appendChild(g1.el); el.appendChild(g2.el);
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8200, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            g1.el.style.transform = 'rotate(' + (elapsed * 0.012).toFixed(2) + 'deg)';
            g2.el.style.transform = 'rotate(' + (-elapsed * 0.012 + 7).toFixed(2) + 'deg)';
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 4; i++) {
                var pool = Math.random() < 0.5 ? g1.spans : g2.spans;
                pool[Math.floor(Math.random() * pool.length)].textContent = artGlyph();
            }
        }, 130);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }

    // ── Variant: VORTEX — spiral inflow; particles drift along the spiral toward center ──
    function spawnArtifactVortex(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var N = 24;
        var particles = [];
        for (var i = 0; i < N; i++) {
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            span.style.fontSize = (9 + Math.random() * 4) + 'px';
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
            particles.push({
                el: span,
                a0: (i / N) * Math.PI * 2,
                r0: 50 + Math.random() * 24,
                speed: 0.7 + Math.random() * 0.5
            });
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8800, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            for (var i = 0; i < particles.length; i++) {
                var p = particles[i];
                var t = elapsed / 1000;
                var ang = p.a0 + t * p.speed;
                var rad = Math.max(2, p.r0 - t * 5.5);
                p.el.style.left = Math.round(Math.cos(ang) * rad) + 'px';
                p.el.style.top  = Math.round(Math.sin(ang) * rad) + 'px';
                p.el.style.opacity = (0.30 + 0.55 * (rad / p.r0)).toFixed(2);
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 3; i++) particles[Math.floor(Math.random() * N)].el.textContent = artGlyph();
        }, 130);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }

    // ── Variant: EXPLOSION — outward burst with deceleration to a slow float ─────
    function spawnArtifactExplosion(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var N = 28;
        var bits = [];
        for (var i = 0; i < N; i++) {
            var ang = Math.random() * Math.PI * 2;
            var dist = 60 + Math.random() * 50;
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            span.style.fontSize = (9 + Math.random() * 4) + 'px';
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
            bits.push({ el: span, dx: Math.cos(ang) * dist, dy: Math.sin(ang) * dist, driftAng: ang + (Math.random() - 0.5) * 0.5 });
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8400, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            // Burst phase 0-700ms: ease-out outward. Then slow float along driftAng.
            var burst = Math.min(1, elapsed / 700);
            var burstE = 1 - Math.pow(1 - burst, 3);
            var floatT = Math.max(0, elapsed - 700) / 1000;
            for (var i = 0; i < bits.length; i++) {
                var b = bits[i];
                var x = b.dx * burstE + Math.cos(b.driftAng) * floatT * 2.5;
                var y = b.dy * burstE + Math.sin(b.driftAng) * floatT * 2.5;
                b.el.style.left = Math.round(x) + 'px';
                b.el.style.top  = Math.round(y) + 'px';
                b.el.style.opacity = (0.85 - floatT * 0.05).toFixed(2);
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 3; i++) bits[Math.floor(Math.random() * N)].el.textContent = artGlyph();
        }, 140);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }

    // ── Variant: ORBIT — concentric rings rotate at golden-ratio period differences ─
    function spawnArtifactOrbit(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var rings = [
            { r: 18, n: 5,  speed: 0.0010 },
            { r: 32, n: 8,  speed: 0.0010 / ART_PHI },
            { r: 48, n: 13, speed: 0.0010 / (ART_PHI * ART_PHI) }
        ];
        var nodes = [];
        var center = document.createElement('span');
        center.className = 'cyberspace-artifact-char';
        center.style.left = '0px'; center.style.top = '0px'; center.style.fontSize = '14px';
        center.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
        center.textContent = artGlyph();
        el.appendChild(center);
        nodes.push(center);
        var orbitals = [];
        rings.forEach(function (ring) {
            for (var i = 0; i < ring.n; i++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.fontSize = Math.max(8, 13 - Math.floor(ring.r / 18)) + 'px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                nodes.push(span);
                orbitals.push({ el: span, r: ring.r, baseAng: (i / ring.n) * Math.PI * 2, speed: ring.speed });
            }
        });
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8800, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            for (var i = 0; i < orbitals.length; i++) {
                var o = orbitals[i];
                var a = o.baseAng + elapsed * o.speed;
                o.el.style.left = Math.round(Math.cos(a) * o.r) + 'px';
                o.el.style.top  = Math.round(Math.sin(a) * o.r) + 'px';
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 2; i++) nodes[Math.floor(Math.random() * nodes.length)].textContent = artGlyph();
        }, 150);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }


    // ── Variant: NETWORK — multi-motion node graph with ghost trails ─────────────
    // Per spawn rolls:
    //   * MOTION ∈ {FLOAT, STATIC, DRIFT, ORBIT, JITTER}
    //       - FLOAT  : each node oscillates around its own anchor on its own axis
    //       - STATIC : nodes don't move at all
    //       - DRIFT  : every node drifts in a shared random direction (formation slides)
    //       - ORBIT  : nodes orbit a shared center at independent radii / speeds / direction
    //       - JITTER : small bounded sine nudges in two dimensions (noisy hover)
    //   * NODES ∈ [3..7] — random graph size
    // Comms are 8-glyph packets that follow the LIVE node positions, and every ~10% of a
    // packet's travel they drop a small "trail dot" at the current path point. Trail dots
    // fade over 1.6s, so repeated traffic between the same nodes makes the web shape
    // emerge as accumulating ghost glyphs.
    function spawnArtifactNetwork(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var MOTION_VARIANTS = ['FLOAT', 'STATIC', 'DRIFT', 'ORBIT', 'JITTER'];
        var MOTION = MOTION_VARIANTS[Math.floor(Math.random() * MOTION_VARIANTS.length)];
        var NODES = 3 + Math.floor(Math.random() * 5); // 3..7
        // Shared per-spawn params for DRIFT and ORBIT
        var driftAng = Math.random() * Math.PI * 2;
        var driftSpeed = 0.012 + Math.random() * 0.014;   // px/ms
        var orbitCx = 0, orbitCy = 0;
        var nodes = [];
        for (var i = 0; i < NODES; i++) {
            var a = (i / NODES) * Math.PI * 2 + Math.random() * 0.4;
            var r = 28 + Math.random() * 22;
            var anchorX = Math.cos(a) * r, anchorY = Math.sin(a) * r;
            var nodeEl = document.createElement('div');
            nodeEl.style.position = 'absolute';
            nodeEl.style.left = '0px'; nodeEl.style.top = '0px';
            var trio = [];
            for (var k = 0; k < 3; k++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = ((k - 1) * 5) + 'px';
                span.style.top  = '0px';
                span.style.fontSize = (k === 1 ? 13 : 10) + 'px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                nodeEl.appendChild(span);
                trio.push(span);
            }
            el.appendChild(nodeEl);
            nodes.push({
                el: nodeEl, glyphs: trio,
                anchorX: anchorX, anchorY: anchorY,
                curX: anchorX, curY: anchorY,
                floatAng: Math.random() * Math.PI * 2,
                floatAmp: 5 + Math.random() * 5,
                floatPeriod: 1800 + Math.random() * 1400,
                floatPhase: Math.random() * Math.PI * 2,
                orbitRadius: r * (0.7 + Math.random() * 0.6),
                orbitAng0: a,
                orbitSpeed: 0.0008 + Math.random() * 0.0010,
                orbitDir: Math.random() < 0.5 ? -1 : 1,
                jitterFreqX: 0.010 + Math.random() * 0.008,
                jitterFreqY: 0.009 + Math.random() * 0.008
            });
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 9500 + Math.random() * 2000, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            for (var i = 0; i < nodes.length; i++) {
                var n = nodes[i];
                if (MOTION === 'FLOAT') {
                    var t = (elapsed / n.floatPeriod) * Math.PI * 2 + n.floatPhase;
                    var off = Math.sin(t) * n.floatAmp;
                    n.curX = n.anchorX + Math.cos(n.floatAng) * off;
                    n.curY = n.anchorY + Math.sin(n.floatAng) * off;
                } else if (MOTION === 'STATIC') {
                    n.curX = n.anchorX; n.curY = n.anchorY;
                } else if (MOTION === 'DRIFT') {
                    var d = elapsed * driftSpeed;
                    n.curX = n.anchorX + Math.cos(driftAng) * d;
                    n.curY = n.anchorY + Math.sin(driftAng) * d;
                } else if (MOTION === 'ORBIT') {
                    var oa = n.orbitAng0 + elapsed * n.orbitSpeed * n.orbitDir;
                    n.curX = orbitCx + Math.cos(oa) * n.orbitRadius;
                    n.curY = orbitCy + Math.sin(oa) * n.orbitRadius;
                } else { // JITTER
                    n.curX = n.anchorX + Math.sin(elapsed * n.jitterFreqX + n.floatPhase) * 4;
                    n.curY = n.anchorY + Math.sin(elapsed * n.jitterFreqY + n.floatPhase * 1.7) * 4;
                }
                n.el.style.transform = 'translate(' + n.curX.toFixed(1) + 'px,' + n.curY.toFixed(1) + 'px)';
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        // Ghost trail dot — sticks around 1.6s while fading, so traffic builds a visible web
        function dropTrailDot(x, y) {
            var dot = document.createElement('span');
            dot.className = 'cyberspace-artifact-char';
            dot.style.left = Math.round(x) + 'px';
            dot.style.top  = Math.round(y) + 'px';
            dot.style.fontSize = '7px';
            dot.style.opacity = '0.62';
            dot.style.transition = 'opacity 1.6s ease';
            dot.textContent = '·';
            el.appendChild(dot);
            requestAnimationFrame(function () { requestAnimationFrame(function () { dot.style.opacity = '0'; }); });
            setTimeout(function () { if (dot.parentNode) dot.parentNode.removeChild(dot); }, 1800);
        }
        // Packet — 8 glyphs trailing the head; drops ghost dots every ~10% of travel
        function sendPacket(fromNode, toNode) {
            if (killed) return;
            var LEN = 8;
            var pkg = document.createElement('div');
            pkg.style.position = 'absolute'; pkg.style.left = '0px'; pkg.style.top = '0px';
            var pktGlyphs = [];
            for (var i = 0; i < LEN; i++) {
                var sp = document.createElement('span');
                sp.className = 'cyberspace-artifact-char';
                sp.style.fontSize = Math.max(7, 13 - i) + 'px';
                sp.style.opacity = (0.95 - i * 0.10).toFixed(2);
                sp.textContent = i === 0 ? artGlyph() : '·';
                pkg.appendChild(sp);
                pktGlyphs.push(sp);
            }
            el.appendChild(pkg);
            var pktStart = performance.now(), TRAVEL = 720;
            var lastDropP = -1;
            function pktStep(ts2) {
                if (killed) { if (pkg.parentNode) pkg.parentNode.removeChild(pkg); return; }
                var p = (ts2 - pktStart) / TRAVEL;
                if (p >= 1) { if (pkg.parentNode) pkg.parentNode.removeChild(pkg); return; }
                if (p > lastDropP + 0.10) {
                    lastDropP = p;
                    var dx = fromNode.curX + (toNode.curX - fromNode.curX) * p;
                    var dy = fromNode.curY + (toNode.curY - fromNode.curY) * p;
                    dropTrailDot(dx, dy);
                }
                for (var i = 0; i < LEN; i++) {
                    var pi = Math.max(0, p - i * 0.06);
                    var x = fromNode.curX + (toNode.curX - fromNode.curX) * pi;
                    var y = fromNode.curY + (toNode.curY - fromNode.curY) * pi;
                    pktGlyphs[i].style.left = Math.round(x) + 'px';
                    pktGlyphs[i].style.top  = Math.round(y) + 'px';
                }
                requestAnimationFrame(pktStep);
            }
            requestAnimationFrame(pktStep);
        }
        var sparker = setInterval(function () {
            var a = nodes[Math.floor(Math.random() * NODES)];
            var b = nodes[Math.floor(Math.random() * NODES)];
            if (a !== b) sendPacket(a, b);
        }, 250);
        var allG = []; for (var i = 0; i < nodes.length; i++) allG.push.apply(allG, nodes[i].glyphs);
        var glitchTimer = setInterval(function () { allG[Math.floor(Math.random() * allG.length)].textContent = artGlyph(); }, 150);
        setTimeout(function () {
            killed = true; clearInterval(glitchTimer); clearInterval(sparker);
            el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0';
            setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
        }, ttl);
    }

    // ── Variant: PHASEFIELD — wide grid; cell brightness phases by (x+y+t) wave ─
    function spawnArtifactPhasefield(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var COLS = 9, ROWS = 5, SP = 11;
        var cells = [];
        for (var r = 0; r < ROWS; r++) {
            for (var c = 0; c < COLS; c++) {
                var lx = (c - (COLS - 1) / 2) * SP;
                var ly = (r - (ROWS - 1) / 2) * SP;
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = Math.round(lx) + 'px';
                span.style.top  = Math.round(ly) + 'px';
                span.style.fontSize = '10px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                el.appendChild(span);
                cells.push({ el: span, c: c, r: r });
            }
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8400, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            var t = elapsed / 400;
            for (var i = 0; i < cells.length; i++) {
                var cell = cells[i];
                var phase = (cell.c + cell.r) * 0.45 - t;
                var v = Math.sin(phase) * 0.5 + 0.5;
                cell.el.style.opacity = (0.20 + v * 0.75).toFixed(2);
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 4; i++) cells[Math.floor(Math.random() * cells.length)].el.textContent = artGlyph();
        }, 150);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }

    // ── Variant: SHATTER — cluster fractures into N pieces that drift apart ──────
    function spawnArtifactShatter(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var SHARDS = 9;
        var shards = [];
        for (var i = 0; i < SHARDS; i++) {
            var shard = document.createElement('div');
            shard.style.position = 'absolute';
            shard.style.left = '0px'; shard.style.top = '0px';
            // Each shard = a 2x2 mini-grid of glyphs
            for (var k = 0; k < 4; k++) {
                var span = document.createElement('span');
                span.className = 'cyberspace-artifact-char';
                span.style.left = ((k % 2) * 7) + 'px';
                span.style.top  = (Math.floor(k / 2) * 7) + 'px';
                span.style.fontSize = '11px';
                span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
                span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                shard.appendChild(span);
            }
            el.appendChild(shard);
            var ang = (i / SHARDS) * Math.PI * 2 + Math.random() * 0.4;
            shards.push({ el: shard, dx: Math.cos(ang) * (40 + Math.random() * 25), dy: Math.sin(ang) * (40 + Math.random() * 25), spin: (Math.random() - 0.5) * 0.6 });
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8400, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            // Hold 600ms, then drift apart with deceleration
            var driftT = Math.max(0, elapsed - 600) / 1000;
            var ease = 1 - Math.pow(1 - Math.min(1, driftT / 4), 3);
            for (var i = 0; i < shards.length; i++) {
                var s = shards[i];
                s.el.style.transform = 'translate(' + (s.dx * ease).toFixed(1) + 'px,' + (s.dy * ease).toFixed(1) + 'px) rotate(' + (driftT * s.spin * 60).toFixed(1) + 'deg)';
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var allG = el.querySelectorAll('.cyberspace-artifact-char');
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 3; i++) allG[Math.floor(Math.random() * allG.length)].textContent = artGlyph();
        }, 150);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }

    // ── Variant: SPIROGRAM — glyphs trail along an epitrochoid (Spirograph) path ──
    function spawnArtifactSpirogram(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        // Epitrochoid params — chosen so the trail makes a visible petal pattern
        var Rc = 22, rc = 6, d = 10;
        var N = 18;
        var trail = [];
        for (var i = 0; i < N; i++) {
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            span.style.fontSize = '10px';
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
            trail.push(span);
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 9000, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            var leadP = elapsed * 0.0040;
            for (var i = 0; i < N; i++) {
                var p = leadP - i * 0.20;
                var x = (Rc + rc) * Math.cos(p) - d * Math.cos(((Rc + rc) / rc) * p);
                var y = (Rc + rc) * Math.sin(p) - d * Math.sin(((Rc + rc) / rc) * p);
                trail[i].style.left = Math.round(x) + 'px';
                trail[i].style.top  = Math.round(y) + 'px';
                trail[i].style.opacity = (0.30 + 0.60 * (1 - i / N)).toFixed(2);
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () { trail[Math.floor(Math.random() * N)].textContent = artGlyph(); }, 120);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }

    // ── Variant: PULSARRING — ring with brightness wave traveling around it ──────
    function spawnArtifactPulsarring(host) {
        var el = artNewContainer();
        el.style.opacity = '0'; el.style.transition = 'opacity 0.55s ease';
        var N = 16, R = 30;
        var ring = [];
        for (var i = 0; i < N; i++) {
            var a = (i / N) * Math.PI * 2;
            var span = document.createElement('span');
            span.className = 'cyberspace-artifact-char';
            span.style.left = Math.round(Math.cos(a) * R) + 'px';
            span.style.top  = Math.round(Math.sin(a) * R) + 'px';
            span.style.fontSize = '12px';
            span.style.animation = 'cyberspace-art-flicker 0.7s steps(1) infinite';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
            ring.push({ el: span, idx: i });
        }
        host.appendChild(el);
        requestAnimationFrame(function () { requestAnimationFrame(function () { el.style.opacity = '1'; }); });
        var startTs = performance.now(), ttl = 8400, killed = false;
        function step(ts) {
            if (killed) return;
            var elapsed = ts - startTs;
            if (elapsed > ttl) return;
            var head = (elapsed / 110) % N;
            for (var i = 0; i < ring.length; i++) {
                var dist = Math.abs(i - head);
                if (dist > N / 2) dist = N - dist;
                var brightness = Math.max(0, 1 - dist / 3);
                var op = (0.25 + brightness * 0.70).toFixed(2);
                var scale = (0.95 + brightness * 0.35).toFixed(2);
                ring[i].el.style.opacity = op;
                ring[i].el.style.transform = 'scale(' + scale + ')';
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
        var glitchTimer = setInterval(function () {
            for (var i = 0; i < 2; i++) ring[Math.floor(Math.random() * N)].el.textContent = artGlyph();
        }, 130);
        setTimeout(function () { killed = true; clearInterval(glitchTimer); el.style.transition = 'opacity 0.4s ease'; el.style.opacity = '0'; setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450); }, ttl);
    }

    // Dispatcher — every spawn rolls a random variant from the enum so the screen cycles
    // through every iteration of how artifacts have ever looked. Pass forceVariant
    // (e.g. 'SPIDER') to bypass the random roll — used by the demo page.
    function spawnArtifact(forceVariant) {
        var host = getHost();
        if (!host) return;
        var variant = forceVariant || ART_VARIANTS[Math.floor(Math.random() * ART_VARIANTS.length)];
        switch (variant) {
            case 'SCATTER':   spawnArtifactScatter(host);   break;
            case 'LATTICE':   spawnArtifactLattice(host);   break;
            case 'ANCHOR':    spawnArtifactAnchor(host);    break;
            case 'SLUG':      spawnArtifactSlug(host);      break;
            case 'CENTIPEDE': spawnArtifactCentipede(host); break;
            case 'PULSE':     spawnArtifactPulse(host);     break;
            case 'WANDERER':  spawnArtifactWanderer(host);  break;
            case 'SPIDER':    spawnArtifactSpider(host);    break;
            case 'INCHWORM':  spawnArtifactInchworm(host);  break;
            case 'HOPPER':    spawnArtifactHopper(host);    break;
            case 'JELLY':     spawnArtifactJelly(host);     break;
            case 'SQUID':     spawnArtifactSquid(host);     break;
            case 'BEETLE':    spawnArtifactBeetle(host);    break;
            case 'TADPOLE':   spawnArtifactTadpole(host);   break;
            case 'ANT':       spawnArtifactAnt(host);       break;
            case 'MOIRE':      spawnArtifactMoire(host);      break;
            case 'VORTEX':     spawnArtifactVortex(host);     break;
            case 'EXPLOSION':  spawnArtifactExplosion(host);  break;
            case 'ORBIT':      spawnArtifactOrbit(host);      break;
            case 'NETWORK':    spawnArtifactNetwork(host);    break;
            case 'PHASEFIELD': spawnArtifactPhasefield(host); break;
            case 'SHATTER':    spawnArtifactShatter(host);    break;
            case 'SPIROGRAM':  spawnArtifactSpirogram(host);  break;
            case 'PULSARRING': spawnArtifactPulsarring(host); break;
        }
    }

    // ── Morse-code glowing dot ────────────────────────────────────────────────
    // A single dot pulses or shifts cardinal directions to spell a short word in Morse.
    var MORSE_TABLE = {
        'A':'.-',   'B':'-...', 'C':'-.-.', 'D':'-..',  'E':'.',    'F':'..-.',
        'G':'--.',  'H':'....', 'I':'..',   'J':'.---', 'K':'-.-',  'L':'.-..',
        'M':'--',   'N':'-.',   'O':'---',  'P':'.--.', 'Q':'--.-', 'R':'.-.',
        'S':'...',  'T':'-',    'U':'..-',  'V':'...-', 'W':'.--',  'X':'-..-',
        'Y':'-.--', 'Z':'--..',
        '0':'-----','1':'.----','2':'..---','3':'...--','4':'....-',
        '5':'.....','6':'-....','7':'--...','8':'---..','9':'----.'
    };
    // Cheat-code-ish message pool — natural-length messages, no length constraint applied.
    var MORSE_MESSAGES = [
        'SOS', 'HELP', 'CTRL', 'NULL', '404', 'KILL', 'BCI', 'GLMZ',
        'JACK', 'WAKE', 'JOIN', 'PING', 'LOST', 'OKAY', 'AWAY',
        'ZERO', 'IRIS', 'CALL', 'STOP', 'BURN', 'IDDQD', 'IDKFA',
        'XYZZY', 'KONAMI', 'UPUPDOWN', 'BBABSEL', 'GHOSTLOG', 'SUDORM',
        'ROOTKIT', 'CHMOD777', 'MORTAL11', 'ABACABB', 'FREEZE',
        'CTRLALT', 'DEBUG'
    ];
    // rgb triplets — the CSS uses rgba(var(--mc), α) so we can pulse alpha in/out
    var MORSE_COLORS = [
        '255, 0, 51',     // red
        '60, 130, 255',   // blue
        '255, 185, 60',   // amber
        '50, 220, 110',   // green
        '220, 225, 230',  // white
        '230, 80, 220',   // magenta
        '60, 220, 230',   // cyan
        '255, 110, 30'    // orange
    ];

    function morseEncode(msg) {
        var out = [];
        for (var i = 0; i < msg.length; i++) {
            var ch = msg[i].toUpperCase();
            if (MORSE_TABLE[ch]) out.push(MORSE_TABLE[ch]);
        }
        return out; // array of letter strings; join with letter-gap, gap inside string is intra-letter gap
    }

    function spawnMorseDot() {
        var host = getHost();
        if (!host) return;

        // Natural-length message — Morse rules apply, no Fibonacci constraint.
        var msg     = pick(MORSE_MESSAGES);
        var color   = pick(MORSE_COLORS);
        var letters = morseEncode(msg);
        if (!letters.length) return;

        // 90% blink, 10% shift. Shift mode is the rarer "this one's typing the cheat-code in motion" variant.
        var mode = Math.random() < 0.10 ? 'shift' : 'blink';
        // Cheat-code pace — ~3× faster than the original. dit ≈ 30–55ms
        var unit = rand(30, 55);

        // Build a flat instruction queue: {kind:'on', units:1|3} or {kind:'gap', units:1|3|7}
        var queue = [];
        for (var li = 0; li < letters.length; li++) {
            var pat = letters[li];
            for (var pi = 0; pi < pat.length; pi++) {
                if (pi > 0) queue.push({ kind: 'gap', units: 1 }); // intra-letter gap
                queue.push({ kind: 'on', units: pat[pi] === '.' ? 1 : 3 });
            }
            if (li < letters.length - 1) queue.push({ kind: 'gap', units: 3 }); // inter-letter gap
        }

        // Outer "orbit" container — handles the slow satellite drift across the sky.
        // The dot inside is positioned at orbit-origin so its own transform (used by shift mode)
        // composes cleanly with the orbit's drift transform. Spawn coords avoid the tile keepout.
        var orbit = document.createElement('div');
        orbit.className = 'cyberspace-morse-orbit';
        var op = safePos(40, 40, 4, 92, 4, 92);
        orbit.style.left = op[0] + '%';
        orbit.style.top  = op[1] + '%';
        // Tiny drift vector — barely moves, but enough that you'd notice if you watched it.
        // 60–140px traveled over 45–80s ⇒ 1–3 px/sec, that "is that a satellite?" pace.
        var driftAngle = Math.random() * Math.PI * 2;
        var driftDist  = 60 + Math.random() * 80;
        orbit.style.setProperty('--ox', Math.round(Math.cos(driftAngle) * driftDist) + 'px');
        orbit.style.setProperty('--oy', Math.round(Math.sin(driftAngle) * driftDist) + 'px');
        orbit.style.animation = 'cyberspace-morse-orbit-drift ' + (45 + Math.random() * 35).toFixed(1) + 's linear forwards';
        host.appendChild(orbit);

        var dot = document.createElement('div');
        dot.className = 'cyberspace-morse-dot';
        dot.style.setProperty('--mc', color);
        // Shift-mode dots stay glowing the whole time; blink-mode dots toggle the .cyberspace-morse-on class
        if (mode === 'shift') dot.classList.add('cyberspace-morse-on');
        orbit.appendChild(dot);

        // Fade in
        dot.style.opacity = '0';
        dot.style.transition = 'opacity 0.4s ease';
        requestAnimationFrame(function () {
            requestAnimationFrame(function () { dot.style.opacity = '1'; });
        });

        var dirs = [[0, -1], [0, 1], [-1, 0], [1, 0]]; // up, down, left, right
        var idx = 0, killed = false;

        function finish() {
            if (killed) return;
            killed = true;
            dot.classList.remove('cyberspace-morse-on');
            dot.style.transition = 'opacity 0.5s ease, transform 0.3s ease';
            dot.style.transform = 'translate(0,0)';
            dot.style.opacity = '0';
            // Tear down the orbit container too — the dot lives inside it
            setTimeout(function () { if (orbit.parentNode) orbit.parentNode.removeChild(orbit); }, 600);
        }

        function step() {
            if (killed) return;
            if (idx >= queue.length) { finish(); return; }
            var s = queue[idx++];
            var dur = s.units * unit;

            if (s.kind === 'gap') {
                if (mode === 'blink') dot.classList.remove('cyberspace-morse-on');
                // shift mode: pause at start position during gap (transform already 0,0)
                setTimeout(step, dur);
                return;
            }

            // s.kind === 'on'
            if (mode === 'blink') {
                dot.classList.add('cyberspace-morse-on');
                setTimeout(function () {
                    if (killed) return;
                    dot.classList.remove('cyberspace-morse-on');
                    step();
                }, dur);
            } else {
                // shift mode — slide in a random cardinal, hold, return; distance encodes dit vs dah.
                // Each shift also swaps the dot's color so motion + chromatic shift mark the symbol.
                var d    = dirs[Math.floor(Math.random() * 4)];
                var dist = s.units === 1 ? 5 : 13; // dit = small, dah = bigger throw
                // u/d/l/r motion is twice as fast as the symbol duration: the dot
                // snaps out, holds at the offset for the bulk of the symbol, then
                // snaps back. Halving out and back (was 0.55 / 0.35) gives the
                // saved time to the hold, so each shift reads as a crisp gesture
                // without changing the underlying morse rhythm.
                var outMs  = Math.round(dur * 0.275);
                var backMs = Math.round(dur * 0.175);
                var holdMs = dur - outMs - backMs;
                // Pick a different color than the current one — guaranteed change every shift
                var nextColor = pick(MORSE_COLORS.filter(function (c) { return c !== dot.style.getPropertyValue('--mc'); }));
                dot.style.setProperty('--mc', nextColor);
                dot.style.transition = 'transform ' + outMs + 'ms ease-out';
                dot.style.transform  = 'translate(' + (d[0] * dist) + 'px,' + (d[1] * dist) + 'px)';
                setTimeout(function () {
                    if (killed) return;
                    setTimeout(function () {
                        if (killed) return;
                        dot.style.transition = 'transform ' + backMs + 'ms ease-in';
                        dot.style.transform  = 'translate(0,0)';
                        setTimeout(step, backMs);
                    }, holdMs);
                }, outMs);
            }
        }

        step();

        // Safety cap — if the queue somehow runs long, force-clean
        var maxLifeMs = queue.reduce(function (a, q) { return a + q.units * unit; }, 0) + 1500;
        setTimeout(finish, maxLifeMs + 800);
    }

    // ── Network connection attempts ───────────────────────────────────────────
    var NET_OCCUPIED = new Set();
    var NET_GRID = 24;

    function netGk(gx, gy) { return gx + ',' + gy; }

    // 8-direction Manhattan segment (hFirst = horizontal before vertical, or vice versa)
    function netRouteManh(x0, y0, x1, y1, hFirst, extra) {
        var path = [[x0, y0]], cx = x0, cy = y0;
        function blocked(gx, gy) { var k = netGk(gx, gy); return NET_OCCUPIED.has(k) || extra.has(k); }
        if (hFirst) {
            var sx = x1 > x0 ? 1 : x1 < x0 ? -1 : 0;
            while (cx !== x1) { cx += sx; if (blocked(cx, cy)) return null; path.push([cx, cy]); }
            var sy = y1 > y0 ? 1 : y1 < y0 ? -1 : 0;
            while (cy !== y1) { cy += sy; if (blocked(cx, cy)) return null; path.push([cx, cy]); }
        } else {
            var sy = y1 > y0 ? 1 : y1 < y0 ? -1 : 0;
            while (cy !== y1) { cy += sy; if (blocked(cx, cy)) return null; path.push([cx, cy]); }
            var sx = x1 > x0 ? 1 : x1 < x0 ? -1 : 0;
            while (cx !== x1) { cx += sx; if (blocked(cx, cy)) return null; path.push([cx, cy]); }
        }
        return path;
    }

    // Diagonal-first segment: 45° leg then cardinal remainder (UR, DR, DL, UL then U/D/L/R)
    function netRouteDiag(x0, y0, x1, y1, extra) {
        var path = [[x0, y0]], cx = x0, cy = y0;
        function blocked(gx, gy) { var k = netGk(gx, gy); return NET_OCCUPIED.has(k) || extra.has(k); }
        var dx = x1 - x0, dy = y1 - y0;
        var sx = dx > 0 ? 1 : dx < 0 ? -1 : 0;
        var sy = dy > 0 ? 1 : dy < 0 ? -1 : 0;
        var diag = Math.min(Math.abs(dx), Math.abs(dy));
        for (var i = 0; i < diag; i++) {
            cx += sx; cy += sy;
            if (blocked(cx, cy)) return null;
            path.push([cx, cy]);
        }
        while (cx !== x1) { cx += sx; if (blocked(cx, cy)) return null; path.push([cx, cy]); }
        while (cy !== y1) { cy += sy; if (blocked(cx, cy)) return null; path.push([cx, cy]); }
        return path;
    }

    // Tron light-cycle routing: right-angle moves only, never diagonal.
    // NET_OCCUPIED guarantees the path can never cross itself or other live wires.
    function netRouteSegment(x0, y0, x1, y1, extra) {
        var hF = Math.random() < 0.5;
        return netRouteManh(x0, y0, x1, y1, hF, extra) || netRouteManh(x0, y0, x1, y1, !hF, extra);
    }

    // Liang-Barsky segment-to-AABB entry: returns t∈[0,1] of first entry, or null
    function netSegEntersBox(ax, ay, bx, by, rx, ry, rw, rh) {
        var dx = bx-ax, dy = by-ay, tMin = 0, tMax = 1;
        function clip(p, q) {
            if (Math.abs(p) < 1e-9) return q >= 0;
            var t = q/p;
            if (p < 0) { if (t > tMin) tMin = t; } else { if (t < tMax) tMax = t; }
            return tMin <= tMax;
        }
        if (!clip(-dx,ax-(rx-rw))) return null; if (!clip(dx,(rx+rw)-ax)) return null;
        if (!clip(-dy,ay-(ry-rh))) return null; if (!clip(dy,(ry+rh)-ay)) return null;
        return tMax >= tMin ? tMin : null;
    }

    // Build a full grid path through 0-3 alternating waypoints (variable directness)
    function netBuildPath(gx0, gy0, gx1, gy1, maxGX, maxGY) {
        for (var attempt = 0; attempt < 14; attempt++) {
            var r = Math.random();
            var numWP = r < 0.40 ? 0 : r < 0.75 ? 1 : r < 0.95 ? 2 : 3;
            var waypts = [[gx0, gy0]];
            for (var w = 0; w < numWP; w++) {
                var f   = (w + 1) / (numWP + 1);
                var bx  = Math.round(gx0 + (gx1 - gx0) * f);
                var by  = Math.round(gy0 + (gy1 - gy0) * f);
                var pdx = -(gy1 - gy0), pdy = gx1 - gx0;
                var plen = Math.sqrt(pdx * pdx + pdy * pdy) || 1;
                var amt  = (w % 2 === 0 ? 1 : -1) * (5 + Math.random() * 9);
                var wx   = Math.max(2, Math.min(maxGX, bx + Math.round(pdx / plen * amt)));
                var wy   = Math.max(2, Math.min(maxGY, by + Math.round(pdy / plen * amt)));
                waypts.push([wx, wy]);
            }
            waypts.push([gx1, gy1]);
            var full = [[gx0, gy0]], ok = true;
            var localSeen = new Set(); localSeen.add(netGk(gx0, gy0));
            for (var i = 0; i < waypts.length - 1; i++) {
                var seg = netRouteSegment(waypts[i][0], waypts[i][1], waypts[i+1][0], waypts[i+1][1], localSeen);
                if (!seg) { ok = false; break; }
                for (var j = 1; j < seg.length; j++) { localSeen.add(netGk(seg[j][0], seg[j][1])); full.push(seg[j]); }
            }
            if (!ok) continue;
            var clean = [full[0]];
            for (var i = 1; i < full.length; i++) {
                if (full[i][0] !== clean[clean.length-1][0] || full[i][1] !== clean[clean.length-1][1])
                    clean.push(full[i]);
            }
            return clean;
        }
        return null;
    }

    // Reduce to direction-change corners (handles all 8 directions)
    function netSimplifyPath(path) {
        if (path.length <= 2) return path.slice();
        var corners = [path[0]];
        for (var i = 1; i < path.length - 1; i++) {
            var dx1 = path[i][0] - path[i-1][0], dy1 = path[i][1] - path[i-1][1];
            var dx2 = path[i+1][0] - path[i][0],  dy2 = path[i+1][1] - path[i][1];
            if (dx1 !== dx2 || dy1 !== dy2) corners.push(path[i]);
        }
        corners.push(path[path.length - 1]);
        return corners;
    }

    var NET_COLORS = [
        { line: 'rgba(255,0,51,0.32)',   node: 'rgba(255,0,51,0.60)',   fail: '#ff2244' },
        { line: 'rgba(60,130,255,0.32)', node: 'rgba(60,130,255,0.60)', fail: '#4488ff' },
        { line: 'rgba(70,210,170,0.32)', node: 'rgba(70,210,170,0.60)', fail: '#46d2aa' },
        { line: 'rgba(255,185,60,0.32)', node: 'rgba(255,185,60,0.60)', fail: '#ffb93c' },
        { line: 'rgba(190,80,255,0.32)', node: 'rgba(190,80,255,0.60)', fail: '#be50ff' },
    ];
    function netSuccessColor(col) { return pick(NET_COLORS.filter(function(c) { return c !== col; })); }

    // 2226-appropriate endpoint IDs — no IP addresses. 100 distinct label formats.
    function netHex(bits) { return Math.floor(Math.random() * Math.pow(2, bits)).toString(16).toUpperCase().padStart(Math.ceil(bits / 4), '0'); }
    function netLetter() { return String.fromCharCode(65 + Math.floor(Math.random() * 26)); }
    var NET_ADDR_FMTS = [
        function() { return 'NODE:'   + netHex(16); },
        function() { return 'RLY-'    + rand(100,999); },
        function() { return 'ARCH:'   + netHex(8)  + '.' + rand(1,9); },
        function() { return 'MEM:0x'  + netHex(16); },
        function() { return 'VX:'     + rand(1,9) + netHex(4) + rand(1,9); },
        function() { return 'SHARD:'  + rand(1000,9999); },
        function() { return 'BCI://'  + netHex(8) + ':' + rand(10,99); },
        function() { return 'RELAY:'  + netHex(4) + rand(10,99); },
        function() { return 'CORE-'   + netHex(8); },
        function() { return 'BRIDGE:' + rand(10,99) + '/' + rand(10,99); },
        function() { return 'HUB::'   + netLetter() + netLetter() + rand(100,999); },
        function() { return 'SPINE-'  + netHex(12); },
        function() { return 'GHOST:'  + netHex(16) + '@' + rand(1,9); },
        function() { return 'FORK→'   + netHex(8); },
        function() { return 'PORT:'   + rand(1024, 65535); },
        function() { return 'AS'      + rand(10000,99999); },
        function() { return 'TOR-'    + netHex(20).slice(0, 12); },
        function() { return 'MESH#'   + netHex(8); },
        function() { return 'DARK:'   + netHex(12); },
        function() { return 'NEXUS-'  + netLetter() + rand(100,999); },
        function() { return 'PULSE:'  + rand(40,200) + 'Hz'; },
        function() { return 'FERRO·'  + netHex(8); },
        function() { return 'KIRIN-'  + rand(1,9) + '.'  + rand(10,99); },
        function() { return 'AMARA:'  + netHex(12); },
        function() { return 'OBSID-'  + netHex(8); },
        function() { return 'KARMA:'  + rand(0,7) + '/' + rand(0,7); },
        function() { return 'STACK['  + rand(0,15) + ']'; },
        function() { return 'PIPE→'   + rand(0,31) + '←' + rand(0,31); },
        function() { return 'THRD:'   + netHex(8); },
        function() { return 'EPHEM:'  + netHex(16); },
        function() { return 'GATE:'   + netLetter() + netLetter() + '-' + rand(10,99); },
        function() { return 'VAULT-'  + netHex(8); },
        function() { return 'VEX:'    + rand(0,9) + ':' + rand(10,99); },
        function() { return 'NULLPTR'; },
        function() { return 'σ-'      + netHex(8); },
        function() { return 'φ-'      + rand(100,999); },
        function() { return 'WORM:'   + netHex(12); },
        function() { return 'ROUTE→'  + netHex(8); },
        function() { return 'TUNNEL[' + rand(0,99) + ']'; },
        function() { return 'CACHE-'  + netHex(6); },
        function() { return 'L2:'     + netHex(8); },
        function() { return 'L3:'     + netHex(8); },
        function() { return 'EDGE:'   + netLetter() + rand(100,999); },
        function() { return 'GHOST['  + netHex(4) + ']'; },
        function() { return 'AETH-'   + netHex(8); },
        function() { return 'SCRIB:'  + rand(1,9) + '/' + rand(10,99); },
        function() { return 'KEEP::'  + netHex(8); },
        function() { return 'MIRROR-' + netHex(6); },
        function() { return 'PYLON#'  + rand(100,999); },
        function() { return 'HYDRA-'  + netLetter() + netLetter() + rand(10,99); },
        function() { return 'DREAD:'  + netHex(8); },
        function() { return 'HALF:'   + netHex(4) + '·' + netHex(4); },
        function() { return 'OBELISK' + rand(1,9); },
        function() { return 'STILL:'  + netHex(6); },
        function() { return 'WHISPR-' + netHex(6); },
        function() { return 'CRYPT:'  + netHex(12); },
        function() { return 'ZERO:'   + netHex(8); },
        function() { return 'COLD:'   + netHex(8); },
        function() { return 'CIPHER#' + rand(1000,9999); },
        function() { return 'DROP-'   + netLetter() + rand(10,99); },
        function() { return 'BURN-'   + netHex(6); },
        function() { return 'FENCE:'  + netHex(8); },
        function() { return 'FANG:'   + netHex(6); },
        function() { return 'WIRE-'   + rand(1,9) + '/' + rand(1,9) + '/' + rand(1,9); },
        function() { return 'LATCH:'  + netHex(8); },
        function() { return 'PHANT-'  + netHex(8); },
        function() { return 'STEEPLE' + rand(10,99); },
        function() { return 'SPIRE-'  + netHex(6); },
        function() { return 'KOBOLD:' + netHex(6); },
        function() { return 'HAUNT:'  + netHex(8); },
        function() { return 'SIGIL#'  + netHex(8); },
        function() { return 'CHANT-'  + rand(100,999); },
        function() { return 'EMBER:'  + netHex(6); },
        function() { return 'OBOL-'   + netHex(6); },
        function() { return 'STRAY-'  + netHex(8); },
        function() { return 'LANTRN:' + rand(10,99); },
        function() { return 'GLITCH#' + netHex(6); },
        function() { return 'SHARDS·' + rand(1,9) + '/' + rand(10,99); },
        function() { return 'BIND:'   + netHex(8); },
        function() { return 'DYE:'    + netHex(6); },
        function() { return 'KEEN-'   + netHex(8); },
        function() { return 'LURE:'   + netHex(8); },
        function() { return 'COWL:'   + netHex(6); },
        function() { return 'GRAVE:'  + netHex(8); },
        function() { return 'WICK-'   + rand(100,999); },
        function() { return 'TINDER#' + netHex(6); },
        function() { return 'KNAVE:'  + netLetter() + netLetter() + rand(10,99); },
        function() { return 'STILE-'  + netHex(6); },
        function() { return 'MOTH:'   + netHex(8); },
        function() { return 'BRIAR-'  + netHex(8); },
        function() { return 'ASHEN:'  + netHex(6); },
        function() { return 'LICH#'   + netHex(8); },
        function() { return 'OUBLI-'  + netHex(6); },
        function() { return 'VOLT:'   + rand(1,99) + 'kV'; },
        function() { return 'NEMA-'   + rand(1,99); },
        function() { return 'FORGE:'  + netHex(8); },
        function() { return 'PALE-'   + netHex(6); },
        function() { return 'WARDEN'  + rand(10,99); },
        function() { return 'RUNE:'   + netHex(6); },
        function() { return 'FLINT-'  + netHex(8); },
        function() { return 'SCYTHE#' + netHex(6); },
        function() { return 'YIELD:'  + rand(0,99) + '%'; },
        function() { return 'SLAB-'   + netHex(6); },
        function() { return 'KILN:'   + netHex(8); },
    ];
    function netAddr() { return pick(NET_ADDR_FMTS)(); }

    var NET_FAIL_MSGS = [
        'CONNECTION COULD NOT BE ESTABLISHED',
        'HANDSHAKE TIMEOUT',
        'ROUTE SATURATED',
        'AUTH REJECTED',
        'SIGNAL LOST',
        'RELAY NODE OFFLINE',
        'ENCRYPTION MISMATCH',
        'TRACEROUTE BLOCKED',
        // 100 additional failure messages
        'KEY EXCHANGE ABORTED',
        'CERT CHAIN INVALID',
        'CERT EXPIRED',
        'CERT REVOKED',
        'TLS DOWNGRADE REJECTED',
        'CIPHER SUITE UNSUPPORTED',
        'PROTOCOL VERSION MISMATCH',
        'UPSTREAM RESET',
        'DOWNSTREAM RESET',
        'PEER CLOSED CONNECTION',
        'KEEPALIVE EXPIRED',
        'TTL EXCEEDED',
        'MTU MISMATCH',
        'FRAGMENT REASSEMBLY FAIL',
        'SEQUENCE GAP',
        'ACK STORM',
        'CONGESTION COLLAPSE',
        'BACKPRESSURE LIMIT',
        'QUEUE OVERFLOW',
        'BUFFER UNDERRUN',
        'RING BUFFER WRAP',
        'TOKEN BUCKET DEPLETED',
        'RATE LIMIT EXCEEDED',
        'QUOTA EXHAUSTED',
        'CIRCUIT BREAKER OPEN',
        'DEADLINE EXCEEDED',
        'OPERATION CANCELLED',
        'CLIENT DISCONNECTED',
        'SERVER UNREACHABLE',
        'GATEWAY UNAVAILABLE',
        'PROXY FAULT',
        'LOAD BALANCER REJECT',
        'BACKEND POOL DRAINED',
        'NO HEALTHY UPSTREAMS',
        'DNS RESOLUTION FAIL',
        'DNS POISONED — REFUSED',
        'BCI NEUROSYNC LOST',
        'CORTEX ATTESTATION FAIL',
        'IDENTITY ASSERTION REJECTED',
        'CLAIMS NOT ENDORSED',
        'TRUST ANCHOR MISSING',
        'CRL FETCH FAIL',
        'OCSP RESPONSE STALE',
        'NONCE REPLAYED',
        'REPLAY DETECTED — DROPPED',
        'INTEGRITY HMAC INVALID',
        'PAYLOAD TAMPERED',
        'CHECKSUM MISMATCH',
        'COMPRESSION BOMB',
        'MALFORMED HEADER',
        'OVERSIZED FRAME',
        'UNDERSIZED FRAME',
        'INVALID OPCODE',
        'STREAM ABORTED',
        'WINDOW UPDATE FROZEN',
        'FLOW CONTROL VIOLATION',
        'GOAWAY RECEIVED',
        'SETTINGS NEGOTIATION FAILED',
        'PUSH PROMISE REJECTED',
        'CONTINUATION FRAME LOST',
        'PRIORITY DEPENDENCY CYCLE',
        'SHARD UNREACHABLE',
        'CONSENSUS LOST — RETRY',
        'QUORUM NOT MET',
        'LEADER ELECTION TIMEOUT',
        'WAL FRAME CORRUPT',
        'JOURNAL TRUNCATED',
        'SNAPSHOT INCOMPLETE',
        'REPLICATION LAG TOO HIGH',
        'PARTITION TOLERANCE BREACHED',
        'SPLIT BRAIN DETECTED',
        'GHOST WRITE OBSERVED',
        'STALE READ — REJECTED',
        'CLOCK SKEW INTOLERABLE',
        'NTP DRIFT > THRESHOLD',
        'EPOCH ROLLED — STALE TOKEN',
        'CARRIER SIGNAL LOST',
        'LINK LAYER DOWN',
        'PHYSICAL LAYER FAULT',
        'OPTICAL LINK ALARM',
        'ICE CONNECTIVITY FAIL',
        'STUN REFLEX ABSENT',
        'TURN ALLOC DENIED',
        'NAT BINDING EXPIRED',
        'CGNAT POOL EXHAUSTED',
        'BGP HIJACK SUSPECTED',
        'AS PATH POISONED',
        'PREFIX WITHDRAWN',
        'JURISDICTIONAL FILTER',
        'GEO-IP BLACKLIST',
        'REGION ACCESS DENIED',
        'SOVEREIGN CHANNEL CLOSED',
        'CORPONATION FIREWALL HOLD',
        'ENCLAVE SEAL FAIL',
        'REMOTE ATTESTATION REJECTED',
        'TPM QUOTE INVALID',
        'SECURE BOOT FAIL',
        'KERNEL MODULE TAINTED',
        'DRIVER PANIC — CONNECTION LOST',
        'INTERRUPT STORM',
        'IRQ DEFERRED FOREVER',
        'DMA REGION INVALID',
        'POWER RAIL BROWNOUT',
        'THERMAL THROTTLE — LINK DOWN',
        'EMP-CLASS INTERFERENCE',
        'JAMMING DETECTED',
        'DECOY RESPONSE — IGNORED',
        'INTERCEPTION CONFIRMED',
        'TRAP NODE — BAILING',
        'TARPIT — ABANDONING',
        'HONEYNET SUSPECTED',
        'KILL-SWITCH ENGAGED',
    ];
    // Sharp-turn sparks — small particle burst at a 90° corner, like a Tron light cycle scraping the wall
    function netSpawnSparks(host, px, py, col) {
        var n = rand(4, 8);
        for (var i = 0; i < n; i++) {
            var s = document.createElement('div');
            s.className = 'cyberspace-net-spark';
            s.style.left  = px + 'px';
            s.style.top   = py + 'px';
            s.style.color = col.fail; // bright variant of the wire colour
            var ang  = Math.random() * Math.PI * 2;
            var dist = rand(14, 32);
            s.style.setProperty('--sx', Math.round(Math.cos(ang) * dist) + 'px');
            s.style.setProperty('--sy', Math.round(Math.sin(ang) * dist) + 'px');
            var dur = rand(220, 420);
            s.style.animationDuration = dur + 'ms';
            host.appendChild(s);
            (function (el) {
                setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, dur + 60);
            })(s);
        }
    }

    // Direction-aware fail message. `dir = {dx, dy}` is the wire's last-segment travel vector.
    // Horizontal travel: text continues horizontally past the failure point in the travel direction.
    // Vertical travel:   text is centered above (going up) or below (going down) the failure point.
    function netShowFail(host, fx, fy, col, elems, gridPath, dir) {
        dir = dir || { dx: 1, dy: 0 };
        var msg = document.createElement('div');
        msg.className = 'cyberspace-net-fail';
        msg.style.color = col.fail;

        // Pick anchor + offset from direction. Inline transform replaces the default centering.
        if (Math.abs(dir.dx) >= Math.abs(dir.dy)) {
            // Horizontal travel — extend the text along the line, vertically centered
            if (dir.dx >= 0) {
                msg.style.left = (fx + 6) + 'px';
                msg.style.top  = fy + 'px';
                msg.style.transform = 'translate(0, -50%)';
                msg.style.textAlign = 'left';
            } else {
                msg.style.left = (fx - 6) + 'px';
                msg.style.top  = fy + 'px';
                msg.style.transform = 'translate(-100%, -50%)';
                msg.style.textAlign = 'right';
            }
        } else {
            // Vertical travel — text centered above or below the failure point
            if (dir.dy >= 0) {
                msg.style.left = fx + 'px';
                msg.style.top  = (fy + 6) + 'px';
                msg.style.transform = 'translate(-50%, 0)';
            } else {
                msg.style.left = fx + 'px';
                msg.style.top  = (fy - 6) + 'px';
                msg.style.transform = 'translate(-50%, -100%)';
            }
            msg.style.textAlign = 'center';
        }

        msg.textContent = pick(NET_FAIL_MSGS);
        host.appendChild(msg);
        setTimeout(function () {
            if (msg.parentNode) msg.parentNode.removeChild(msg);
            elems.forEach(function (el) {
                el.style.transition = 'opacity 0.4s ease';
                el.style.opacity = '0';
                setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 450);
            });
            gridPath.forEach(function (p) { NET_OCCUPIED.delete(netGk(p[0], p[1])); });
        }, 1100);
    }

    // Expanding ping ring — fires at every successful corner, neutral marker
    function netSpawnPing(host, px, py, col, depth) {
        depth = depth || 0;
        var el = document.createElement('div');
        el.className = 'cyberspace-net-ping';
        el.style.borderColor = col.node;
        el.style.left = px + 'px';
        el.style.top  = py + 'px';
        host.appendChild(el);
        setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, 750);
        if (depth < 2 && Math.random() < 0.18) {
            setTimeout(function () { netSpawnTracer(host, px, py, col, depth + 1); }, rand(200, 700));
        }
    }

    // Mean glyphs the interception/predator swarm cycles through
    var NET_WASP_CHARS = '#@!*&%§¶†‡¦¤¥×÷±¬¿¡';

    // ── Artifact predator swarm — RARE event ────────────────────────────────────
    // The swarm is no longer a network-failure mechanic. It hunts the LARGEST artifact on
    // screen, consuming and converting its cells one at a time until nothing remains. Bails
    // silently if there's no suitable target. Should feel like a surprise every time.
    function spawnArtifactPredator() {
        var host = getHost();
        if (!host) return;
        // Find the largest artifact on screen by cell count
        var artifacts = host.querySelectorAll('.cyberspace-artifact');
        var target = null, mostCells = 0;
        artifacts.forEach(function (a) {
            var c = a.querySelectorAll('.cyberspace-artifact-char').length;
            if (c > mostCells) { mostCells = c; target = a; }
        });
        if (!target || mostCells < 16) return; // nothing big enough — silent bail

        // Mark the artifact as under attack — its own despawn timer will skip removal
        // while this flag is set. The predator removes the empty container itself when
        // every cell has been consumed.
        target.__predated = true;

        var hostRect = host.getBoundingClientRect();
        var tRect    = target.getBoundingClientRect();
        var ax = tRect.left - hostRect.left + tRect.width  / 2;
        var ay = tRect.top  - hostRect.top  + tRect.height / 2;

        // Live cell pool — references to actual DOM nodes still inside the artifact
        var cellPool = Array.prototype.slice.call(target.querySelectorAll('.cyberspace-artifact-char'));
        var n = Math.min(cellPool.length, rand(8, 14));

        // Swarm origin — distant point off the artifact
        var origAng  = Math.random() * Math.PI * 2;
        var origDist = 220 + Math.random() * 200;
        var ox = Math.max(20, Math.min((host.offsetWidth  || 800) - 20, ax + Math.cos(origAng) * origDist));
        var oy = Math.max(20, Math.min((host.offsetHeight || 600) - 20, ay + Math.sin(origAng) * origDist));

        // ── Prey detection + panic response ────────────────────────────────────
        // The artifact has a directional detection cone — fastest in its direction of travel,
        // slowest behind it. If the predator's origin lies within the cone, the artifact panics
        // and redirects its crawl vector to flee AWAY from the predator at increased magnitude.
        var artCdx = parseFloat(target.style.getPropertyValue('--cdx')) || 0;
        var artCdy = parseFloat(target.style.getPropertyValue('--cdy')) || 0;
        var travelMag = Math.hypot(artCdx, artCdy);
        var apdx = ox - ax, apdy = oy - ay;     // from artifact center to predator origin
        var apMag = Math.hypot(apdx, apdy);
        var alignDot = 0;
        if (travelMag > 0 && apMag > 0) {
            alignDot = (artCdx / travelMag) * (apdx / apMag)
                     + (artCdy / travelMag) * (apdy / apMag);
            // alignDot ≈ +1: predator straight ahead (in direction of travel) → max detection
            // alignDot ≈ -1: predator straight behind                         → min detection
        }
        var maxDetRadius = 420;   // detection range when predator is straight ahead
        var minDetRadius = 100;   // detection range when predator is behind
        var detRadius = minDetRadius + (maxDetRadius - minDetRadius) * (alignDot * 0.5 + 0.5);
        if (apMag > 0 && apMag < detRadius) {
            // PANIC: redirect crawl AWAY from predator. The current keyframe animation re-evaluates
            // its var()-based keyframes when --cdx/--cdy change, so the artifact lurches into the
            // new flee direction at the next frame — reads as a startled bolt.
            var fleeMag = 360;
            var fleeX = (-apdx / apMag) * fleeMag;
            var fleeY = (-apdy / apMag) * fleeMag;
            target.style.setProperty('--cdx', Math.round(fleeX) + 'px');
            target.style.setProperty('--cdy', Math.round(fleeY) + 'px');
        }

        var PRED_COLOR = '255, 34, 68'; // rgb triplet for the wasp red
        var wasps = [];
        for (var i = 0; i < n; i++) {
            var w = document.createElement('span');
            w.className = 'cyberspace-net-wasp';
            w.style.color = '#ff2244';
            w.style.fontSize = rand(11, 16) + 'px';
            for (var k = 1; k <= 4; k++) {
                var jang = Math.random() * Math.PI * 2;
                var jr   = 4 + Math.random() * 10;
                w.style.setProperty('--bx' + k, Math.round(Math.cos(jang) * jr) + 'px');
                w.style.setProperty('--by' + k, Math.round(Math.sin(jang) * jr) + 'px');
            }
            w.style.setProperty('--bd', rand(80, 140) + 'ms');
            w.textContent = NET_WASP_CHARS[Math.floor(Math.random() * NET_WASP_CHARS.length)];
            var jx = (Math.random() - 0.5) * 36;
            var jy = (Math.random() - 0.5) * 36;
            w.style.left = (ox + jx) + 'px';
            w.style.top  = (oy + jy) + 'px';
            host.appendChild(w);
            wasps.push({ el: w, x: ox + jx, y: oy + jy, target: null });
        }

        // Pacing knobs — tuned to feel deliberate rather than violent.
        // swarmSpeed lower = wasps glide instead of zipping; feedMs holds each wasp on
        // its kill for a beat so the dissolve has visible weight; the cell-fade itself
        // is also stretched (was 0.18s/200ms).
        var swarmSpeed   = 480; // px/s — slower glide toward each target
        var arriveDist   = 10;
        var feedMs       = 240; // wasp idles on the just-consumed cell before picking next
        var consumeFade  = 0.45; // s — cell opacity transition during dissolve
        var consumeRemove = 520; // ms — DOM removal delay (slightly > consumeFade)
        var lastTs = null;
        var done = false;

        function step(ts) {
            if (done) return;
            if (!lastTs) { lastTs = ts; requestAnimationFrame(step); return; }
            var dt = Math.min(0.05, (ts - lastTs) / 1000);
            lastTs = ts;
            var hr = host.getBoundingClientRect();
            for (var i = 0; i < wasps.length; i++) {
                var w = wasps[i];
                // Honor feeding pause — wasp idles on the just-consumed cell before
                // moving on. Reads as "digestion" instead of an immediate dart-away.
                if (w.feedingUntil) {
                    if (ts < w.feedingUntil) continue;
                    w.feedingUntil = 0;
                }
                // Pick a fresh target if needed
                if (!w.target || !w.target.parentNode) {
                    if (cellPool.length === 0) { w.target = null; continue; }
                    var idx = Math.floor(Math.random() * cellPool.length);
                    w.target = cellPool.splice(idx, 1)[0];
                }
                // Re-read target position each frame — artifacts crawl/undulate, target moves
                var cr = w.target.getBoundingClientRect();
                if (cr.width === 0 && cr.height === 0) {
                    // Target already detached — drop it and pick next frame
                    w.target = null;
                    continue;
                }
                var tx = cr.left - hr.left + cr.width  / 2;
                var ty = cr.top  - hr.top  + cr.height / 2;
                var dx = tx - w.x, dy = ty - w.y;
                var dist = Math.hypot(dx, dy);
                if (dist <= arriveDist) {
                    // Consume + convert: cell adopts a wasp glyph + the signature wasp red,
                    // then slowly dissolves. The longer fade + feeding pause give the kill
                    // visible weight without the original snap.
                    var c = w.target;
                    c.textContent = NET_WASP_CHARS[Math.floor(Math.random() * NET_WASP_CHARS.length)];
                    c.style.color = '#ff2244';
                    c.style.transition = 'opacity ' + consumeFade + 's ease';
                    c.style.opacity = '0';
                    setTimeout((function (cellEl) { return function () {
                        if (cellEl.parentNode) cellEl.parentNode.removeChild(cellEl);
                    }; })(c), consumeRemove);
                    w.target = null;
                    w.feedingUntil = ts + feedMs;
                    continue;
                }
                var advance = Math.min(dist, swarmSpeed * dt);
                w.x += (dx / dist) * advance;
                w.y += (dy / dist) * advance;
                w.el.style.left = w.x + 'px';
                w.el.style.top  = w.y + 'px';
            }

            // Done when every cell is gone AND every wasp has cleared its current target
            var allDone = (cellPool.length === 0) && wasps.every(function (w) { return !w.target; });
            if (!allDone) { requestAnimationFrame(step); return; }
            done = true;
            // Disperse wasps off in random directions and fade them out
            wasps.forEach(function (w) {
                var dispAng = Math.random() * Math.PI * 2;
                var dispD   = 60 + Math.random() * 80;
                w.el.style.transition = 'left 0.45s ease-out, top 0.45s ease-out, opacity 0.45s ease';
                w.el.style.left = (w.x + Math.cos(dispAng) * dispD) + 'px';
                w.el.style.top  = (w.y + Math.sin(dispAng) * dispD) + 'px';
                w.el.style.opacity = '0';
                setTimeout(function () { if (w.el.parentNode) w.el.parentNode.removeChild(w.el); }, 480);
            });
            // Remove the now-empty artifact container
            setTimeout(function () { if (target.parentNode) target.parentNode.removeChild(target); }, 500);
        }
        requestAnimationFrame(step);
    }

    // Hostile interception swarm — originates OFF the wire and HOMES on a moving target.
    // `getTarget()` returns the current target point (x, y) — typically the LATEST corner the
    // wire has reached. As the swarm flies, it scans every corner in `corners`; when its centroid
    // gets within `awareRadius` of an undiscovered corner, `onAware(idx, x, y)` fires (the swarm
    // "becomes aware" of that corner — caller fires a ping and marks it for the wire's redraw).
    // `onArrive()` fires when the swarm catches up to the moving target (severs the wire).
    function netSpawnInterception(host, getTarget, corners, col, onAware, onArrive) {
        var hostW = host.offsetWidth || 800, hostH = host.offsetHeight || 600;
        var n = rand(8, 14);
        // Initial swarm origin — random distant point relative to the *current* target
        var initial = getTarget();
        var origAngle = Math.random() * Math.PI * 2;
        var origDist  = 200 + Math.random() * 200;
        var swarmX = Math.max(20, Math.min(hostW - 20, initial.x + Math.cos(origAngle) * origDist));
        var swarmY = Math.max(20, Math.min(hostH - 20, initial.y + Math.sin(origAngle) * origDist));

        var wasps = [];
        for (var i = 0; i < n; i++) {
            var w = document.createElement('span');
            w.className = 'cyberspace-net-wasp';
            w.style.color = col.fail;
            w.style.fontSize = rand(10, 16) + 'px';
            for (var k = 1; k <= 4; k++) {
                var ang = Math.random() * Math.PI * 2;
                var r   = 4 + Math.random() * 10;
                w.style.setProperty('--bx' + k, Math.round(Math.cos(ang) * r) + 'px');
                w.style.setProperty('--by' + k, Math.round(Math.sin(ang) * r) + 'px');
            }
            w.style.setProperty('--bd', rand(80, 140) + 'ms');
            w.textContent = NET_WASP_CHARS[Math.floor(Math.random() * NET_WASP_CHARS.length)];
            // Per-wasp offset around swarm centroid — gives the cluster width without breaking the formation
            var ox = (Math.random() - 0.5) * 32;
            var oy = (Math.random() - 0.5) * 32;
            w.style.left = (swarmX + ox) + 'px';
            w.style.top  = (swarmY + oy) + 'px';
            host.appendChild(w);
            wasps.push({ el: w, ox: ox, oy: oy });
        }

        // Swarm pace: faster than the typical wire speed (720–1100 px/s), so it always catches up.
        // The wire's speed is its pace ALONG the path; the swarm's speed is straight-line — so even
        // before doing the math, straight-line beats path-following with turns. 1500 px/s is plenty.
        var swarmSpeed   = 1500;
        var arriveDist   = 16;
        var awareRadius  = 70;  // detection radius — swarm "becomes aware" of corners within this range
        var awareR2      = awareRadius * awareRadius;
        var lastTs = null, arrived = false;

        function step(ts) {
            if (arrived) return;
            if (!lastTs) { lastTs = ts; requestAnimationFrame(step); return; }
            var dt = Math.min(0.05, (ts - lastTs) / 1000);
            lastTs = ts;

            // Awareness scan — every undiscovered corner within radius gets discovered this frame.
            // The wire keeps appearing dot-less until the swarm sweeps past; then dots bloom along
            // the path, like the swarm is mapping the route as it hunts.
            if (corners) {
                for (var ci = 0; ci < corners.length; ci++) {
                    var c = corners[ci];
                    if (c.discovered) continue;
                    var pdx = c.x - swarmX, pdy = c.y - swarmY;
                    if (pdx * pdx + pdy * pdy <= awareR2) {
                        c.discovered = true;
                        onAware(c.idx, c.x, c.y);
                    }
                }
            }

            var t = getTarget();
            var dx = t.x - swarmX, dy = t.y - swarmY;
            var dist = Math.hypot(dx, dy);
            if (dist <= arriveDist) {
                arrived = true;
                wasps.forEach(function (w) {
                    w.el.style.transition = 'opacity 0.3s ease';
                    w.el.style.opacity = '0';
                    setTimeout(function () { if (w.el.parentNode) w.el.parentNode.removeChild(w.el); }, 320);
                });
                onArrive();
                return;
            }
            var advance = Math.min(dist, swarmSpeed * dt);
            swarmX += (dx / dist) * advance;
            swarmY += (dy / dist) * advance;
            for (var i = 0; i < wasps.length; i++) {
                wasps[i].el.style.left = (swarmX + wasps[i].ox) + 'px';
                wasps[i].el.style.top  = (swarmY + wasps[i].oy) + 'px';
            }
            requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }

    // Small tracer that homes toward (tx, ty) and fires a ping on arrival
    function netSpawnTracer(host, tx, ty, col, depth) {
        var hostW = host.offsetWidth || 800, hostH = host.offsetHeight || 600;
        var angle = Math.random() * Math.PI * 2;
        var dist  = rand(80, 160);
        var sx = Math.max(10, Math.min(hostW - 10, Math.round(tx + Math.cos(angle) * dist)));
        var sy = Math.max(10, Math.min(hostH - 10, Math.round(ty + Math.sin(angle) * dist)));
        var el = document.createElement('div');
        el.className = 'cyberspace-net-tracer';
        el.style.color = col.node;
        el.style.left = sx + 'px';
        el.style.top  = sy + 'px';
        el.textContent = '\u25C9'; // circled dot
        host.appendChild(el);
        var ms = rand(500, 1200);
        requestAnimationFrame(function() {
            requestAnimationFrame(function() {
                el.style.transition = 'left ' + ms + 'ms linear, top ' + ms + 'ms linear, opacity 0.2s ease ' + Math.max(0, ms - 200) + 'ms';
                el.style.left = tx + 'px';
                el.style.top  = ty + 'px';
                el.style.opacity = '0';
            });
        });
        setTimeout(function() {
            if (!el.__predated && el.parentNode) el.parentNode.removeChild(el);
            netSpawnPing(host, tx, ty, col, depth);
        }, ms + 60);
    }

    // Plan a Tron light-cycle path between two boxes. The result is fully deterministic before
    // the line ever leaves the source: a Fibonacci number of turns, monotonic in each axis (so
    // the wire CANNOT double back on itself), entering the destination box at the exact midpoint
    // of one of its four sides.
    var NET_FIB_TURNS = [1, 3, 5, 13]; // odd Fibs only — odd K gives K+1 segments alternating H/V cleanly

    // Split a signed total into n monotonic pieces, all non-zero in the same direction,
    // every piece a DISTINCT integer multiple of minMag, and not equal to any value in `forbidden`.
    // Each piece's magnitude is multiplier × minMag where multipliers are unique positive integers.
    // This produces "circuit board"-style runs: short stubs, long traces, no two segments the same.
    function netSplitMono(total, n, minMag, forbidden) {
        forbidden = forbidden || [];
        var forbidUnits = forbidden.map(function (f) { return Math.round(Math.abs(f) / minMag); });
        var sign = total < 0 ? -1 : 1;
        var totalUnits = Math.round(Math.abs(total) / minMag);
        if (n === 1) return [total];

        // Minimum sum for n distinct positive integers is 1+2+…+n. If the budget is tight,
        // we shrink minMag-equivalent to whatever fits — caller's job to size the boxes generously.
        var baseSum = n * (n + 1) / 2;
        if (totalUnits < baseSum) {
            // Degenerate: fall back to the simple staircase (1, 2, …, n-1, remainder)
            var pieces = [];
            var rem = totalUnits;
            for (var i = 0; i < n - 1; i++) {
                var p = Math.min(i + 1, rem - (n - i - 1));
                p = Math.max(1, p);
                pieces.push(sign * p * minMag);
                rem -= p;
            }
            pieces.push(sign * Math.max(1, rem) * minMag);
            return pieces;
        }

        // Start from the minimal distinct set [1, 2, …, n] and distribute the surplus by
        // bumping random multipliers (only when the new value stays unique and not forbidden).
        var mults = [];
        for (var i = 1; i <= n; i++) mults.push(i);
        var surplus = totalUnits - baseSum;
        var guard = 4000;
        while (surplus > 0 && guard-- > 0) {
            var idx = Math.floor(Math.random() * n);
            var newVal = mults[idx] + 1;
            if (mults.indexOf(newVal) >= 0 || forbidUnits.indexOf(newVal) >= 0) continue;
            mults[idx] = newVal;
            surplus--;
        }
        // Dump any leftover surplus onto the current maximum (preserves uniqueness)
        if (surplus > 0) {
            var mi = 0;
            for (var i = 1; i < n; i++) if (mults[i] > mults[mi]) mi = i;
            mults[mi] += surplus;
        }
        // Shuffle so segment lengths don't appear in monotonic order along the path
        for (var i = mults.length - 1; i > 0; i--) {
            var j = Math.floor(Math.random() * (i + 1));
            var tmp = mults[i]; mults[i] = mults[j]; mults[j] = tmp;
        }
        var pieces = mults.map(function (m) { return sign * m * minMag; });
        // Absorb the integer-rounding gap into the last piece so sum(pieces) === total exactly.
        // Without this, the final segment can pick up a few sub-pixel diagonals from rounding.
        var sum = pieces.reduce(function (a, b) { return a + b; }, 0);
        var diff = total - sum;
        pieces[pieces.length - 1] += diff;
        return pieces;
    }

    // Pick a target Fibonacci K. We require this EXACT K to fit — no graceful fallback. If the
    // chosen K's segment count doesn't fit in the available budget, the planner returns null
    // and the connection fails (every wire has its limit of corners; geometry that can't honor
    // it doesn't downgrade — it severs).
    function netPickFibTurns(totalH, totalV, minSeg) {
        // Pick at random from the Fibonacci ladder — every connection rolls its own complexity.
        // Bias toward higher values so the screen is mostly circuitous, but small Ks still appear.
        var K = NET_FIB_TURNS[Math.floor(Math.random() * NET_FIB_TURNS.length)];
        var nPer = (K + 1) / 2;
        if (Math.abs(totalH) < nPer * minSeg || Math.abs(totalV) < nPer * minSeg) return -1;
        return K;
    }

    // Plan the wire. Returns { pts, K, srcSide, dstSide } where pts[0] is the source-side
    // midpoint and pts[last] is the destination-side midpoint — both perfectly centered.
    function netPlanFibPath(p1, sw, sh, p2, bw, bh) {
        var dxRaw = p2[0] - p1[0], dyRaw = p2[1] - p1[1];
        // Default first-segment direction is the dominant displacement axis — most direct route.
        // ~30% of attempts deliberately START on the OPPOSITE axis, so the wire heads "the wrong
        // way" first and has to correct itself with later segments. If that direction's geometry
        // can't accommodate the chosen K, the connection gets lost (the wire couldn't recover).
        var dominantH = Math.abs(dxRaw) >= Math.abs(dyRaw);
        var firstH = Math.random() < 0.30 ? !dominantH : dominantH;
        var srcSide, dstSide, srcExit, dstEntry;
        if (firstH) {
            srcSide  = dxRaw > 0 ? 'R' : 'L';
            dstSide  = dyRaw >= 0 ? 'T' : 'B';
            srcExit  = srcSide === 'R' ? [p1[0] + sw, p1[1]] : [p1[0] - sw, p1[1]];
            dstEntry = dstSide === 'T' ? [p2[0], p2[1] - bh] : [p2[0], p2[1] + bh];
        } else {
            srcSide  = dyRaw > 0 ? 'D' : 'U';
            dstSide  = dxRaw >= 0 ? 'L' : 'R';
            srcExit  = srcSide === 'D' ? [p1[0], p1[1] + sh] : [p1[0], p1[1] - sh];
            dstEntry = dstSide === 'L' ? [p2[0] - bw, p2[1]] : [p2[0] + bw, p2[1]];
        }
        var totalH = dstEntry[0] - srcExit[0];
        var totalV = dstEntry[1] - srcExit[1];
        var minSeg = NET_GRID; // 24 px — visual grid spacing
        var K = netPickFibTurns(totalH, totalV, minSeg);
        // Connection fails outright if the rolled corner count can't fit the available geometry —
        // no downgrading to a simpler path. The wire either is what it was supposed to be, or breaks.
        if (K < 0) return null;
        var nPer = (K + 1) / 2;
        // Generate H pieces first; pass them as forbidden when generating V pieces so that
        // NO two segments anywhere in the wire share a length. The result reads as a circuitous
        // PCB trace, never a regular grid step.
        var hPieces = netSplitMono(totalH, nPer, minSeg, []);
        var vPieces = netSplitMono(totalV, nPer, minSeg, hPieces);
        var pts = [srcExit.slice()];
        var cx = srcExit[0], cy = srcExit[1];
        if (firstH) {
            for (var i = 0; i < nPer; i++) {
                cx += hPieces[i]; pts.push([cx, cy]);
                cy += vPieces[i]; pts.push([cx, cy]);
            }
        } else {
            for (var i = 0; i < nPer; i++) {
                cy += vPieces[i]; pts.push([cx, cy]);
                cx += hPieces[i]; pts.push([cx, cy]);
            }
        }
        // Snap final point exactly to the planned midpoint — kills any rounding drift so the wire
        // truly enters dead-centre on the destination side.
        pts[pts.length - 1] = dstEntry.slice();
        // Validate: every segment must be purely H or purely V — no diagonals allowed.
        // If rounding ever produced a segment that changes both axes, the connection is lost.
        for (var vi = 1; vi < pts.length; vi++) {
            var sdx = pts[vi][0] - pts[vi - 1][0];
            var sdy = pts[vi][1] - pts[vi - 1][1];
            if (Math.abs(sdx) > 0.5 && Math.abs(sdy) > 0.5) return null;
        }
        return { pts: pts, K: K, srcSide: srcSide, dstSide: dstSide };
    }

    // Single-connection lock — only one network wire effect is allowed on screen at a time.
    // Cleared by every terminal path (success fade-out, planner failure, willFail trigger).
    var NET_BUSY = false;

    function spawnNetConnect() {
        if (NET_BUSY) return;
        var host = getHost();
        if (!host) return;
        NET_BUSY = true;
        var hostW = host.offsetWidth  || 800;
        var hostH = host.offsetHeight || 600;
        var col = pick(NET_COLORS);
        var marg = 70;
        // Convert tile keepout rects to host-relative coords once for this attempt loop
        var hostRect = host.getBoundingClientRect();
        var keepout = getKeepoutRects().map(function (r) {
            return {
                left:   r.left   - hostRect.left,
                right:  r.right  - hostRect.left,
                top:    r.top    - hostRect.top,
                bottom: r.bottom - hostRect.top
            };
        });
        function pairBoundingBox(a, b) {
            return {
                left:   Math.min(a[0], b[0]),
                right:  Math.max(a[0], b[0]),
                top:    Math.min(a[1], b[1]),
                bottom: Math.max(a[1], b[1])
            };
        }
        function pairCrossesKeepout(a, b) {
            // Wire is monotonic Manhattan, so its full extent stays within the bounding box of
            // its endpoints. If THAT box overlaps any tile rect, the wire would cross tiles.
            var bb = pairBoundingBox(a, b);
            for (var k = 0; k < keepout.length; k++) {
                var kc = keepout[k];
                if (bb.left < kc.right && bb.right > kc.left && bb.top < kc.bottom && bb.bottom > kc.top) {
                    return true;
                }
            }
            return false;
        }
        var p1, p2, att = 0;
        do {
            p1 = [rand(marg, hostW - marg), rand(marg, hostH - marg)];
            p2 = [rand(marg, hostW - marg), rand(marg, hostH - marg)];
            att++;
        } while ((Math.abs(p1[0]-p2[0]) + Math.abs(p1[1]-p2[1]) < 220 || pairCrossesKeepout(p1, p2)) && att < 24);
        // If no clear placement was found, abort cleanly so we don't draw across tiles
        if (pairCrossesKeepout(p1, p2)) { NET_BUSY = false; return; }

        function makeBox(px, py, opaque) {
            var el = document.createElement('div');
            el.className = 'cyberspace-net-node';
            el.style.color = col.node; el.style.borderColor = col.node;
            el.style.left = px + 'px'; el.style.top = py + 'px';
            el.style.opacity = opaque ? '1' : '0.15';
            el.textContent = netAddr();
            return el;
        }
        var box1 = makeBox(p1[0], p1[1], true);
        var box2 = makeBox(p2[0], p2[1], false);
        host.appendChild(box1); host.appendChild(box2);

        // Measure boxes — DOM has to lay them out before we can pull offsetWidth/Height.
        var bw1 = (box1.offsetWidth  || 40) * 0.5, bh1 = (box1.offsetHeight || 14) * 0.5;
        var bw2 = (box2.offsetWidth  || 40) * 0.5, bh2 = (box2.offsetHeight || 14) * 0.5;

        // Plan the entire wire BEFORE the line ever exits the source node.
        // Fibonacci turn count, monotonic-per-axis (no doubling back), centered destination entry.
        // The planner can return null (geometry doesn't fit, or wrong-axis-bias chose impossibly).
        // SEVER (CONNECTION LOST) should ONLY fire after the wire has actually started drawing.
        // So: retry the planner up to 8× with re-rolled randomness; if every attempt fails, this
        // attempt is silently aborted (boxes removed, NET_BUSY released — no fail message).
        var plan = null;
        for (var pAtt = 0; pAtt < 8; pAtt++) {
            plan = netPlanFibPath(p1, bw1, bh1, p2, bw2, bh2);
            if (plan && plan.pts && plan.pts.length >= 2) break;
            plan = null;
        }
        if (!plan) {
            // Couldn't plan a route at all — silent abort, no SEVER message (nothing was drawn).
            if (box1.parentNode) box1.parentNode.removeChild(box1);
            if (box2.parentNode) box2.parentNode.removeChild(box2);
            NET_BUSY = false;
            return;
        }
        var pts = plan.pts;
        var gridPath = []; // legacy occupancy hook — empty since the new planner doesn't use NET_OCCUPIED
        var canvas = document.createElement('canvas');
        canvas.width = hostW; canvas.height = hostH;
        canvas.style.cssText = 'position:absolute;left:0;top:0;pointer-events:none;z-index:1;';
        host.appendChild(canvas);
        var ctx = canvas.getContext('2d');
        var totalLen = 0;
        for (var i = 1; i < pts.length; i++)
            totalLen += Math.hypot(pts[i][0] - pts[i-1][0], pts[i][1] - pts[i-1][1]);
        var speed    = rand(280, 460); // slower trace pace — easier to follow as it lays
        // A wire can only fail if it has at least 3 corners. K=1 (simple L) wires always succeed —
        // there's not enough geometry to "break" mid-route in a way that reads as interesting.
        var willFail = plan.K >= 3 && Math.random() < 0.30;
        var failCornerIdx = willFail && pts.length >= 5
            ? 1 + Math.floor(Math.random() * (pts.length - 3))
            : -1;
        var wireKilled = false;
        var segIdx = 0, segProg = 0, lastTs = null;
        // Speed ramp — the trace starts at 20% of cruise and linearly accelerates
        // to full cruise speed over rampMs, then holds. Reads as the line "getting
        // up to speed" instead of snapping into motion at full velocity.
        var startTs = null;
        var rampMs  = 700;
        var startSpeedFrac = 0.20;
        var curLine = col.line, curNode = col.node;
        var iStart = 2, iEnd = pts.length - 2;
        function redraw() {
            ctx.clearRect(0, 0, hostW, hostH);
            ctx.lineWidth = 1; ctx.strokeStyle = curLine; ctx.fillStyle = curNode;
            // Wire — drawn segment-by-segment, no corner markers ever (the wire is just a wire)
            for (var i = 0; i < segIdx; i++) {
                ctx.beginPath(); ctx.moveTo(pts[i][0], pts[i][1]); ctx.lineTo(pts[i+1][0], pts[i+1][1]); ctx.stroke();
            }
            if (segIdx < pts.length - 1) {
                var tx = pts[segIdx][0] + (pts[segIdx+1][0] - pts[segIdx][0]) * segProg;
                var ty = pts[segIdx][1] + (pts[segIdx+1][1] - pts[segIdx][1]) * segProg;
                ctx.beginPath(); ctx.moveTo(pts[segIdx][0], pts[segIdx][1]); ctx.lineTo(tx, ty); ctx.stroke();
            }
        }
        function doSuccess() {
            canvas.style.opacity = '1';
            redraw();
            var sc = netSuccessColor(col);
            curLine = sc.line; curNode = sc.node;
            box2.style.transition = 'opacity 0.12s ease, color 0.25s ease, border-color 0.25s ease';
            box2.style.opacity = '1';
            box2.style.color = sc.node; box2.style.borderColor = sc.node;
            box1.style.transition = 'color 0.25s ease, border-color 0.25s ease';
            box1.style.color = sc.node; box1.style.borderColor = sc.node;
            redraw();
            // Brief settle, then blink the wire + both nodes three times in lock-step, then fade
            // them all together at the same rate so the connection vanishes as a single unit.
            setTimeout(function () {
                var blinkMs = 600;
                [canvas, box1, box2].forEach(function (el) {
                    // Clear the cyberspace-net-in entry animation so the blink can take over cleanly.
                    el.style.animation = 'none';
                    void el.offsetHeight; // force reflow — guarantees the new animation restarts
                    el.style.transition = ''; // animation drives opacity, no transition fight
                    el.style.animation = 'cyberspace-net-success-blink ' + blinkMs + 'ms linear forwards';
                });
                setTimeout(function () {
                    var fadeMs = 800;
                    [canvas, box1, box2].forEach(function (el) {
                        el.style.animation  = 'none';
                        el.style.transition = 'opacity ' + fadeMs + 'ms ease';
                        el.style.opacity    = '0';
                        setTimeout(function () { if (!el.__predated && el.parentNode) el.parentNode.removeChild(el); }, fadeMs + 60);
                    });
                    gridPath.forEach(function (p) { NET_OCCUPIED.delete(netGk(p[0], p[1])); });
                    // Release the single-connection lock once the fade is done — next attempt allowed
                    setTimeout(function () { NET_BUSY = false; }, fadeMs + 80);
                }, blinkMs + 40);
            }, rand(250, 600));
        }
        function tick(ts) {
            if (wireKilled) return;
            if (!lastTs) lastTs = ts;
            if (!startTs) startTs = ts;
            var dt = Math.min((ts - lastTs) / 1000, 0.1);
            lastTs = ts;
            if (segIdx >= pts.length - 1) { doSuccess(); return; }
            var segLen = Math.hypot(pts[segIdx+1][0] - pts[segIdx][0], pts[segIdx+1][1] - pts[segIdx][1]);
            if (segLen < 1) { segIdx++; segProg = 0; requestAnimationFrame(tick); return; }
            // Linear speed ramp from startSpeedFrac → 1.0 over rampMs, then hold.
            var rampT = Math.min(1, (ts - startTs) / rampMs);
            var speedMul = startSpeedFrac + (1 - startSpeedFrac) * rampT;
            var advance = dt * (speed * speedMul) / segLen;
            segProg = Math.min(1, segProg + advance);
            if (segProg >= 1) {
                segProg = 0; segIdx++;
                // The wire fails the moment it reaches the doomed corner — line halts, the message
                // appears at that point, aligned to the direction it was traveling.
                if (willFail && segIdx === failCornerIdx) {
                    wireKilled = true;
                    var fx = pts[segIdx][0], fy = pts[segIdx][1];
                    // Last-segment direction: the leg that just landed at this corner.
                    var prev = pts[segIdx - 1];
                    var dir = { dx: fx - prev[0], dy: fy - prev[1] };
                    redraw();
                    netShowFail(host, fx, fy, col, [box1, box2, canvas], gridPath, dir);
                    setTimeout(function () { NET_BUSY = false; }, 1600);
                    return;
                }
                // ARC — sharp-turn spark on ~16.5% of interior corners (every grid corner is a 90° turn)
                if (segIdx > 0 && segIdx < pts.length - 1 && Math.random() < 0.165) {
                    netSpawnSparks(host, pts[segIdx][0], pts[segIdx][1], col);
                }
            }
            redraw();
            requestAnimationFrame(tick);
        }
        requestAnimationFrame(tick);
    }

    // ── Spawn rate constants — edit here to tune both hosts identically ────────
    // ── Effect toggles — set to false to disable an effect entirely ─────────
    var FX_ERROR    = true;   // fatal error popups
    var FX_WARN     = true;   // warning popups
    var FX_MEMO     = true;   // corporate memo intercepts
    var FX_GEO      = true;   // geometric schematic windows
    var FX_CASCADE  = true;   // cascading console window burst
    var FX_ARTIFACT = true;   // floating glyph artifact clusters
    var FX_FRAG     = true;   // floating code fragments
    var FX_NET      = true;   // network connection attempts (Tron-cycle wire routing)
    var FX_MORSE    = true;   // glowing Morse-code dots
    var FX_FOLDER   = true;   // folder-rip browser windows
    var FX_PREDATOR = true;   // RARE artifact-predator swarm (consumes large artifacts)
    var FX_WIN      = true;   // console windows (black / blue / amber)

    // ── Spawn rate constants — edit here to tune both hosts identically ────────
    var RATE_ERROR    = 0.01;  // fatal error popups
    var RATE_WARN     = 0.01;  // warning popups
    var RATE_MEMO     = 0.04;  // corporate memo intercepts
    var RATE_GEO      = 0.115; // geometric schematic windows (+15% over baseline 0.10)
    var RATE_CASCADE  = 0.03;  // cascading console window burst
    var RATE_ARTIFACT = 0.12;  // floating glyph artifact clusters
    var RATE_FRAG     = 0.40;  // floating code fragments
    var RATE_NET      = 0.08;  // network connection attempts
    var RATE_MORSE    = 0.05;  // glowing Morse-code dots
    var RATE_FOLDER   = 0.04;  // folder-rip browser windows
    var RATE_PREDATOR = 0.05;  // artifact-predator swarm — bumped from 0.012 for more visible hunts
    // RATE_WIN = remainder — console windows (black / blue / amber)

    function tickDelay() {
        var area = window.innerWidth * window.innerHeight;
        var scale = Math.max(0.35, Math.min(2.5, (1920 * 1080) / area));
        // 15% lower spawn frequency = 1/(1-0.15) ≈ 1.176× longer delay between ticks.
        return rand(590, 2120) * scale * 1.176;
    }

    function tick() {
        if (!getHost()) { tickTimer = null; return; }
        // Stop spawning when the tab is hidden — background timers get throttled to
        // ~1Hz (and worse under Chrome intensive throttling), which used to let
        // effects pile up faster than they could despawn. visibilitychange below
        // restarts the loop when the tab is visible again.
        if (document.hidden) { tickTimer = null; return; }
        var r = Math.random(), t = 0;
        if      (FX_ERROR    && r < (t += RATE_ERROR))    spawnError();
        else if (FX_WARN     && r < (t += RATE_WARN))     spawnWarning();
        else if (FX_MEMO     && r < (t += RATE_MEMO))     spawnMemo();
        else if (FX_GEO      && r < (t += RATE_GEO))      spawnGeoWindow();
        else if (FX_CASCADE  && r < (t += RATE_CASCADE))  spawnCascade();
        else if (FX_ARTIFACT && r < (t += RATE_ARTIFACT)) spawnArtifact();
        else if (FX_FRAG     && r < (t += RATE_FRAG))     spawnFrag();
        else if (FX_NET      && r < (t += RATE_NET))      spawnNetConnect();
        else if (FX_MORSE    && r < (t += RATE_MORSE))    spawnMorseDot();
        else if (FX_FOLDER   && r < (t += RATE_FOLDER))   spawnFolderRip();
        else if (FX_PREDATOR && r < (t += RATE_PREDATOR)) spawnArtifactPredator();
        else if (FX_WIN)                                   spawnWindow();
        tickTimer = setTimeout(tick, tickDelay());
    }

    function start() {
        var host = getHost();
        if (!host) return;
        initTextures();           // always reinit textures on this host
        if (tickTimer) return;    // tick loop already running
        tickTimer = setTimeout(tick, rand(500, 1500));
    }

    // Pause / resume the auto-spawn tick loop. Used by the demo page so the host can
    // isolate a single manually-spawned effect without background noise.
    function setAutoSpawn(on) {
        if (on) {
            start();              // start() is idempotent — no-op when already ticking
        } else if (tickTimer) {
            clearTimeout(tickTimer);
            tickTimer = null;
        }
    }

    // Initial start
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }

    // Restart whenever the tab becomes visible again (tick() bails out on
    // document.hidden to prevent throttled-timer pile-up).
    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) start();
    });

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

    // _demo: internal-but-public spawn handles for the demo/observation page.
    // Underscore-prefixed to signal "dev tool, not production API". Safe to ignore;
    // accessing these has no side effect until you invoke them.
    return {
        start: start,
        _demo: {
            ART_VARIANTS: ART_VARIANTS,
            SacredGeometry: (typeof window !== 'undefined') ? window.SacredGeometry : null,
            spawnArtifact: spawnArtifact,
            spawnGeoWindow: spawnGeoWindow,
            spawnError: spawnError,
            spawnWarning: spawnWarning,
            spawnMemo: spawnMemo,
            spawnFrag: spawnFrag,
            spawnNetConnect: spawnNetConnect,
            spawnMorseDot: spawnMorseDot,
            spawnFolderRip: spawnFolderRip,
            spawnArtifactPredator: spawnArtifactPredator,
            spawnWindow: spawnWindow,
            spawnCascade: spawnCascade,
            setAutoSpawn: setAutoSpawn
        }
    };
})();
