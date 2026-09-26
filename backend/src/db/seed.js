const bcrypt = require('bcryptjs');
const db = require('./index');

function hash(pw) {
  return bcrypt.hashSync(pw, 10);
}

function run() {
  const existingUsers = db.prepare('SELECT COUNT(*) AS c FROM users').get().c;
  if (existingUsers > 0) {
    console.log('Database already has data — skipping seed. Delete backend/data/parking.db to reseed.');
    return;
  }

  console.log('Seeding demo data...');

  const insertUser = db.prepare(
    'INSERT INTO users (name, email, phone, password_hash, role) VALUES (?, ?, ?, ?, ?)'
  );

  const adminId = insertUser.run('System Admin', 'admin@smartparking.com', '9800000000', hash('Admin@123'), 'admin').lastInsertRowid;
  const owner1Id = insertUser.run('Ramesh Shrestha', 'owner1@smartparking.com', '9801111111', hash('Owner@123'), 'parking_owner').lastInsertRowid;
  const owner2Id = insertUser.run('Sita Maharjan', 'owner2@smartparking.com', '9802222222', hash('Owner@123'), 'parking_owner').lastInsertRowid;
  const userId = insertUser.run('Piyush Shrestha', 'user@smartparking.com', '9803333333', hash('User@123'), 'user').lastInsertRowid;
  insertUser.run('Anisha Gurung', 'anisha@smartparking.com', '9804444444', hash('User@123'), 'user');

  const insertArea = db.prepare(
    `INSERT INTO parking_areas
     (owner_id, name, address, city, latitude, longitude, description, price_per_hour, opening_time, closing_time, vehicle_types)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
  );

  const areas = [
    {
      owner: owner1Id,
      name: 'City Center Mall Parking',
      address: 'Kamalpokhari, Kathmandu',
      city: 'Kathmandu',
      lat: 27.7089, lng: 85.3247,
      desc: 'Multi-level covered parking beside City Center Mall. CCTV monitored.',
      price: 50, open: '07:00', close: '22:00', vt: 'car,bike', slots: 12,
    },
    {
      owner: owner1Id,
      name: 'New Road Business Parking',
      address: 'New Road, Kathmandu',
      city: 'Kathmandu',
      lat: 27.7040, lng: 85.3090,
      desc: 'Open-air parking close to New Road shopping street. High demand during peak hours.',
      price: 40, open: '06:00', close: '21:00', vt: 'car,bike', slots: 10,
    },
    {
      owner: owner2Id,
      name: 'Thamel Tourist Parking Hub',
      address: 'Thamel, Kathmandu',
      city: 'Kathmandu',
      lat: 27.7154, lng: 85.3123,
      desc: 'Secure parking hub for tourists and visitors near Thamel.',
      price: 60, open: '00:00', close: '23:59', vt: 'car,bike', slots: 8,
    },
    {
      owner: owner2Id,
      name: 'Patan Durbar Square Parking',
      address: 'Mangal Bazaar, Lalitpur',
      city: 'Lalitpur',
      lat: 27.6727, lng: 85.3247,
      desc: 'Heritage-area parking with easy access to Patan Durbar Square.',
      price: 35, open: '06:00', close: '20:00', vt: 'car,bike', slots: 10,
    },
    {
      owner: owner1Id,
      name: 'Pulchowk Office Park',
      address: 'Pulchowk, Lalitpur',
      city: 'Lalitpur',
      lat: 27.6788, lng: 85.3157,
      desc: 'Weekday office-hours parking for the Pulchowk corporate corridor.',
      price: 45, open: '08:00', close: '19:00', vt: 'car', slots: 9,
    },
  ];

  const insertSlot = db.prepare(
    'INSERT INTO slots (parking_area_id, slot_number, vehicle_type) VALUES (?, ?, ?)'
  );

  for (const a of areas) {
    const areaId = insertArea
      .run(a.owner, a.name, a.address, a.city, a.lat, a.lng, a.desc, a.price, a.open, a.close, a.vt)
      .lastInsertRowid;

    const bikeCount = Math.floor(a.slots * 0.3);
    for (let i = 1; i <= a.slots; i++) {
      const isBike = a.vt.includes('bike') && i <= bikeCount;
      insertSlot.run(areaId, `${isBike ? 'B' : 'A'}${String(i).padStart(2, '0')}`, isBike ? 'bike' : 'car');
    }
  }

  console.log('Seed complete.');
  console.log('---------------------------------------------');
  console.log('Demo accounts (password shown for grading/demo purposes only):');
  console.log('  Admin:          admin@smartparking.com / Admin@123');
  console.log('  Parking Owner:  owner1@smartparking.com / Owner@123 (City Center, New Road, Pulchowk)');
  console.log('  Parking Owner:  owner2@smartparking.com / Owner@123 (Thamel, Patan)');
  console.log('  User:           user@smartparking.com / User@123');
  console.log('---------------------------------------------');
}

run();
